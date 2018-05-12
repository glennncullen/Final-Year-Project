using System;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;
using PubNubAPI;

namespace Traffic_Control_Scripts.Communication
{
    public class Handler
    {
        // pubnub variables
        private static PNConfiguration _pnConfiguration = new PNConfiguration();
        private static Dictionary<string, object> _message = new Dictionary<string, object>();
        private static PubNub _pubnub;
        private static List<string> _channels;
        private static Handler _instance;
        
        
        // game variable
        public static bool IsSomethingOnFire = false;
        public static List<WaypointPath> Path = new List<WaypointPath>();
        public static Building BuildingOnFire;
        
        

        // empty constructor for singleton
        private Handler() {}

        // get the instance
        public static Handler Instance
        {
            get 
            {
                if (_instance != null) return _instance;
                Connect();
                _instance = new Handler();
                return _instance;
            }
        }


        private static void Connect()
        {
            _pnConfiguration.SubscribeKey = "sub-c-ec33873a-53d1-11e8-84ad-b20235bcb09b";
            _pnConfiguration.PublishKey = "pub-c-56bfd71d-e6e9-479d-9c08-b2c719d6a4c7";
            _pnConfiguration.SecretKey = "sec-c-OWY1ZDU0NGUtN2IyZC00YmJmLWFmNTEtOTc3NDFkYWE0YjUw";
            _pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;
            _pnConfiguration.UUID = "shmart-city-unity";
            
            _pubnub = new PubNub(_pnConfiguration);

            _pubnub.SusbcribeCallback += Callback;

            // subscribe to these channels
            _channels = new List<string>()
            {
                "route-to-fire"
            };
            _pubnub.Subscribe().Channels(_channels).Execute();
        }

        
        // call back 
        private static void Callback(object sender, EventArgs e)
        { 
            SusbcribeEventEventArgs messageEvent = e as SusbcribeEventEventArgs;

            if (messageEvent == null) return;
            if (messageEvent.MessageResult == null) return;
            Debug.Log("message received");
            Dictionary<string, object> message = messageEvent.MessageResult.Payload as Dictionary<string, object>;
            if (message == null) return;
            if (messageEvent.MessageResult.Channel.Equals("route-to-fire"))
            {
                Path.Clear();
                foreach (string road in (string[]) message["path"])
                {
                    Debug.Log(road);
                    WaypointPath roadWp = GameObject.Find(road).GetComponent<WaypointPath>();
                    Path.Add(roadWp);
                }
                IsSomethingOnFire = true;
                UpdatePath();
            }
        }
        
        
        // update paths so traffic lights know when to go red
        public static void UpdatePath()
        {
            foreach (WaypointPath road in Path)
            {
                if (road.TrafficLights == null) continue;
                road.TrafficLights.isOnPath = true;
                road.TrafficLights.GetComponentInParent<LightsController>().allRed = true;
            }
        }
        
        
        // get the next path for the firebrigade and inform other systems
        public static Transform GetNextPath()
        {
            if (Path[0].TrafficLights != null)
            {
                Path[0].TrafficLights.isOnPath = false;
                Path[0].TrafficLights.GetComponentInParent<LightsController>().allRed = true;
            }
            Path.Remove(Path[0]);
            Transform nextPath = Path[0].GetComponent<Transform>();
            return nextPath;
        }
        
        
        // inform other systems that firebrigade has moved to next street
        public static void InformMove()
        {
            if (Path.Count == 1)  // crisis averted
            {
                if (Path[0].TrafficLights != null)
                {
                    Path[0].TrafficLights.isOnPath = false;
                    Path[0].TrafficLights.GetComponentInParent<LightsController>().allRed = true;
                }
                Path.Clear();
                IsSomethingOnFire = false;
                BuildingOnFire.ExtinguishFire();
                BuildingOnFire = null;
                _instance.PublishMessage("fire-extinguished", new Dictionary<string, object>(){
                {
                    "extinguished",
                    true
                }});
            }
            else
            {
                _instance.PublishMessage("update-position", new Dictionary<string, object>(){
                {
                    "next",
                    true
                }});
            }
        }

        
        // look at the path coming up
        public string LookAhead()
        {
            return Path.Count < 2 ? "no more streets to check" : Path[1].gameObject.name;
        }

        // send a message throught pub nub
        public void PublishMessage(String channel, Dictionary<string, object> messageToSend)
        {
            _pubnub.Publish()
                .Channel(channel)
                .Message(messageToSend)
                .Async((result, status) => {
                    if (!status.Error) return;
                    Debug.Log(status.Error);
                    Debug.Log(status.ErrorData.Info);
            });
        }
        
    }
}
