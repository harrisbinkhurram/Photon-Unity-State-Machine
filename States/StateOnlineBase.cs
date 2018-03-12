using System.Collections;
using Photon;
using UnityEngine;
using GemsFrontier;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace M3PUN {
    public class StateOnlineBase : StateBase  {
        protected int reconnectRetryCount = 0;
        protected int MAX_RETRIES = 3;
        protected bool reconnecting = false;
        protected DisconnectCause disconnectionCause;
        protected string roomName;
        protected string challengeMode;
        protected bool matchFound = false;
        protected TypedLobby typedLobby;
        protected RoomOptions roomOptions;
        

        public override void OnStateEnter() {
            //Set Lobby Type

            ConnectionController.Instance.M3OnConnectedToMaster += _onConnectedToMaster;
            ConnectionController.Instance.M3OnConnectionFail += _onConnectionFail;
            ConnectionController.Instance.M3OnFailedToConnectToPhoton += _onConnectionFailedToConnect;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnectedFromPhoton;

            base.OnStateEnter();
        }

        public override void OnStateExit() {
            ConnectionController.Instance.M3OnConnectedToMaster -= _onConnectedToMaster;
            ConnectionController.Instance.M3OnConnectionFail -= _onConnectionFail;
            ConnectionController.Instance.M3OnFailedToConnectToPhoton -= _onConnectionFailedToConnect;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnectedFromPhoton;
            base.OnStateExit();
        }
        
        protected void DoJoinLobby() {
            ConnectionController.Instance.JoinLobby();
        }
        
        protected virtual void DoInitNewRoomOptionsAndJoinRoom() {
            matchFound = false;
            
            //Set Room name
            roomName = System.Guid.NewGuid().ToString();

            //Set Room Options
            
            roomOptions = new RoomOptions () { 
                MaxPlayers = M3PUN.Utils.CTECH_PUN_MAX_PLAYERS,
                EmptyRoomTtl = M3PUN.Utils.CTECH_PUN_EMPTY_ROOM_TTL,
                PlayerTtl = M3PUN.Utils.CTECH_PUN_INACTIVE_PLAYER_TTL,
                IsVisible = true,
                IsOpen = true   //should always be open for joining, only close while we have a partner in it, or during battle
            };
            string[] propertiesForLobby = { Utils.CTECH_PUN_ROOM_PROPERTIES_TROPHIES, Utils.CTECH_PUN_ROOM_PROPERTIES_ARENA, Utils.CTECH_PUN_ROOM_USER_NAME, Utils.CTECH_PUN_ROOM_CHALLENGE_MODE, Utils.CTECH_PUN_GUILD_NAME };
            
            UserProfile up = GameState.Instance.Player.profile;
            string guildName = up.Guild != null ? up.Guild.Name : Utils.CTECH_PUN_NO_GUILD_NAME;
            roomOptions.CustomRoomProperties = new Hashtable() { 
                {Utils.CTECH_PUN_ROOM_PROPERTIES_TROPHIES, up.Trophies},
                {Utils.CTECH_PUN_ROOM_PROPERTIES_ARENA, up.ArenaLevel},
                {Utils.CTECH_PUN_ROOM_USER_NAME, up.UserId},
                {Utils.CTECH_PUN_ROOM_CHALLENGE_MODE, challengeMode},
                {Utils.CTECH_PUN_GUILD_NAME, guildName}
            };

            roomOptions.CustomRoomPropertiesForLobby = propertiesForLobby;

            DoJoinRoom();
        }

        protected virtual void DoJoinRoom() {
            ConnectionController.Instance.JoinOrCreateRoom(roomName, roomOptions);
        }


        protected void DoUpdateRoomProperty(Events.PhotonRoomPropertiesUpdate propset) {
            DoUpdateRoomProperty(propset.Property, propset.Value);
        }

        protected void DoUpdateRoomProperty(string key, string value) {
            if(ConnectionController.Instance.ConnectedAndOnline &&
                ConnectionController.Instance.inRoom &&
                 ConnectionController.Instance.room != null
            ){
                ConnectionController.Instance.room.SetCustomProperties( new Hashtable() {{key, value}});
            }
        }

        void DoTransitionToOnlineState() {
            StateMachine.Instance.MakeTransition(typeof(StateOnline));
        }
        
        void DoConnectAfterTokenExpire() {
            ConnectionController.Instance.Connect();
        }

        
        protected virtual void _onConnectedToMaster() {
            ConnectionController.Instance.FetchServerTimeStamp();
            reconnectRetryCount = 0;
            reconnecting = false;
            if(disconnectionCause == DisconnectCause.AuthenticationTicketExpired || disconnectionCause == DisconnectCause.InvalidAuthentication) {
                disconnectionCause = 0;
                StateMachine.Instance.MakeTransition(typeof(StateInitialize));
            } else {
                disconnectionCause = 0;
            }
        }

        protected virtual void tryToReconnect() {
            if(reconnectRetryCount > MAX_RETRIES) {
                if(LoadingView.Instance.IsVisible) {
                    LoadingView.Instance.HideTransition();
                }
                EventListenerForPrivateChallenge.ShowDisconnectedNoInternetConnectivity();
                VersusScreen.Instance.StopAnimation();
                StateMachine.Instance.MakeTransition(typeof(StateOffline));
            } else {
                reconnecting = ConnectionController.Instance.ReconnectAndRejoin();
                if(!reconnecting) {
                    if(!ConnectionController.Instance.Reconnect()) {
                        ConnectionController.Instance.Connect();
                    }
                }
            }

        }
        
        protected virtual void _onDisconnectedFromPhoton() {

            switch(disconnectionCause) {
                case DisconnectCause.ExceptionOnConnect:
                case DisconnectCause.Exception:
                case DisconnectCause.InvalidRegion:
                case DisconnectCause.SecurityExceptionOnConnect:
                //Show Error Try to Reconnect
                Helpers.Utility.LogError("Cannot Connect: " + disconnectionCause.ToString() + " Class: " +  this.GetType().Name);
                ConnectionController.Instance.Connect();
                disconnectionCause = 0;
                break;

                case DisconnectCause.DisconnectByServerUserLimit:
                case DisconnectCause.MaxCcuReached:
                //Go Offline
                tryToReconnect();
                break;

                case DisconnectCause.AuthenticationTicketExpired:
                case DisconnectCause.InvalidAuthentication:
                if(reconnectRetryCount > 3) {
                    if(LoadingView.Instance.IsVisible) {
                        LoadingView.Instance.HideTransition();
                    }
                    EventListenerForPrivateChallenge.ShowDisconnectedNoInternetConnectivity();
                    VersusScreen.Instance.StopAnimation();
                    StateMachine.Instance.MakeTransition(typeof(StateOffline));
                } else {
                    LoadingView.Instance.ShowTransition();
                    Invoke("DoConnectAfterTokenExpire", 1.0f);
                }
                break;

                case DisconnectCause.DisconnectByServerTimeout:
                case DisconnectCause.DisconnectByServerLogic:
                case DisconnectCause.DisconnectByClientTimeout:
                case DisconnectCause.InternalReceiveException:
                default:
                tryToReconnect();
                break;

                }
        }

        protected virtual void _onConnectionFail(DisconnectCause cause) {    
            Helpers.Utility.LogMessage("_onConnectionFail: " + cause.ToString() + " Class: " +  this.GetType().Name);            
            disconnectionCause = cause;
            reconnectRetryCount++;
        }
        
        protected virtual void _onConnectionFailedToConnect(DisconnectCause cause) {
            Helpers.Utility.LogError("_onConnectionFailedToConnect: " + cause.ToString() + " Class: " +  this.GetType().Name);            
            disconnectionCause = cause;
            reconnectRetryCount++;
            _onDisconnectedFromPhoton();
        }
    }
}