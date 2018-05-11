using System;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;
using PubNubAPI;

namespace Traffic_Control_Scripts.Communication
{
    public class PubNubBehaviour
    {
        
        private static PNConfiguration _pnConfiguration = new PNConfiguration();
        private static Dictionary<string, object> _message = new Dictionary<string, object>();
        private static PubNub _pubnub;
        private static List<string> _channels;
        private static PubNubBehaviour _instance;

        // empty constructor for singleton
        private PubNubBehaviour() {}

        // get the instance
        public static PubNubBehaviour Instance
        {
            get 
            {
                if (_instance != null) return _instance;
                Connect();
                _instance = new PubNubBehaviour();
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
                "ambulance-instructions", 
                "traffic-light-instructions",
                "route-to-fire"
            };
            _pubnub.Subscribe().Channels(_channels).Execute();
        }

        
        // call back 
        private static void Callback(object sender, EventArgs e)
        { 
            SusbcribeEventEventArgs messageEvent = e as SusbcribeEventEventArgs;

            if (messageEvent == null) return;
            if (messageEvent.MessageResult != null) {
                Dictionary<string, object> message = messageEvent.MessageResult.Payload as Dictionary<string, object>;
                if (message == null) return;
                Debug.Log("msg: " + message["msg"]);
                if (messageEvent.MessageResult.Channel.Equals("route-to-fire"))
                {
                    Debug.Log("path:\t" + message["path"]);
                }
                else if (messageEvent.MessageResult.Channel.Equals("your_channel"))
                {
                    Debug.Log("YOUR");
                }
            }
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
