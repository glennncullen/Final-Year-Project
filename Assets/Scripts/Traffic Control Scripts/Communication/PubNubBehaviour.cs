using System;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;
using PubNubAPI;

namespace Traffic_Control_Scripts.Communication
{
    public class PubNubBehaviour : MonoBehaviour
    {
        private PNConfiguration _pnConfiguration = new PNConfiguration();
        private Dictionary<string, string> _message = new Dictionary<string, string>();
        private PubNub _pubnub;


        private void Awake()
        {
            _pnConfiguration.SubscribeKey = "sub-c-ec33873a-53d1-11e8-84ad-b20235bcb09b";
            _pnConfiguration.PublishKey = "pub-c-56bfd71d-e6e9-479d-9c08-b2c719d6a4c7";
            _pnConfiguration.SecretKey = "sec-c-OWY1ZDU0NGUtN2IyZC00YmJmLWFmNTEtOTc3NDFkYWE0YjUw";
            _pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;
            _pnConfiguration.UUID = "shmart-city-unity";
            
            _message.Add("msg", "hello");
            _pubnub = new PubNub(_pnConfiguration);

            _pubnub.SusbcribeCallback += (sender, e) =>
            {
                SusbcribeEventEventArgs mea = e as SusbcribeEventEventArgs;

                if (mea.Status != null)
                {
                    if (mea.Status.Category.Equals(PNStatusCategory.PNConnectedCategory))
                    {
                        _pubnub.Publish()
                            .Channel("my_channel")
                            .Message(_message)
                            .Async((result, status) =>
                            {
                                if (!status.Error)
                                {
                                    Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}",
                                        DateTime.UtcNow, result.Timetoken));
                                }
                                else
                                {
                                    Debug.Log(status.Error);
                                    Debug.Log(status.ErrorData.Info);
                                }
                            });
                    }
                }

                if (mea.MessageResult != null)
                {
                    Dictionary<string, object> msg = mea.MessageResult.Payload as Dictionary<string, object>;
                    Debug.Log("msg: " + msg["msg"]);
                }

                if (mea.PresenceEventResult != null)
                {
                    Debug.Log("In Example, SusbcribeCallback in presence" + mea.PresenceEventResult.Channel +
                              mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
                }
            };
            
            
            _pubnub.Subscribe().Channels(new List<string>(){"my_channel"}).Execute();
        }

        
        // call back 
        private void Callback(object sender, EventArgs e)
        { 
            SusbcribeEventEventArgs mea = e as SusbcribeEventEventArgs;
            
            if (mea.Status != null) {
                if (mea.Status.Category.Equals(PNStatusCategory.PNConnectedCategory)) {
                    _pubnub.Publish()
                        .Channel("my_channel")
                        .Message(_message)
                        .Async((result, status) => {
                            if (!status.Error) {
                                Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", DateTime.UtcNow , result.Timetoken));
                            } else {
                                Debug.Log(status.Error);
                                Debug.Log(status.ErrorData.Info);
                            }
                        });
                }
            }
            if (mea.MessageResult != null) {
                Debug.Log("In Example, SusbcribeCallback in message" + mea.MessageResult.Channel);
                Dictionary<string, string> msg = mea.MessageResult.Payload as Dictionary<string, string>;
                Debug.Log("msg: " + msg["msg"]);
            }
            if (mea.PresenceEventResult != null) {
                Debug.Log("In Example, SusbcribeCallback in presence" + mea.PresenceEventResult.Channel + mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
            }
        }
        
    }
}
