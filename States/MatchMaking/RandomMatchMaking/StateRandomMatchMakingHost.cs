using M3PUN;
using Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using GemsFrontier;

namespace M3PUN {
    public class StateRandomMatchMakingHost : StateOnlineBase {

        bool tryToReconnect = false;
        bool showWarning = false;

        public override void OnStateEnter() {
            base.OnStateEnter();
            challengeMode = Utils.STR_RANDOM;
            VersusScreen.Instance.StartAnimation();
            M3GameCache.SetBattleType(BattleType.RandomPVP);
            ConnectionController.Instance.M3OnJoinedRoom += _onJoinedRoom;
            ConnectionController.Instance.M3OnPhotonJoinRoomFailed += _onJoinRoomFailed;
            ConnectionController.Instance.M3OnPhotonPlayerConnected += _onPhotonPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected += _onPhotonPlayerDisconnected;

            Events.EventManager.Instance.UpdatePhotonRoomPropertiesHandler += _onUpdateRoomProperties;
            // ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnectedFromPhoton;

            DoWaitForBattlePartner();
        }
        
        public override void OnStateExit() {
            ConnectionController.Instance.M3OnJoinedRoom -= _onJoinedRoom;
            ConnectionController.Instance.M3OnPhotonJoinRoomFailed -= _onJoinRoomFailed;
            ConnectionController.Instance.M3OnPhotonPlayerConnected -= _onPhotonPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected -= _onPhotonPlayerDisconnected;

            Events.EventManager.Instance.UpdatePhotonRoomPropertiesHandler -= _onUpdateRoomProperties;
            // ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnectedFromPhoton;
            base.OnStateExit();
        }

        void DoWaitForBattlePartner() {
            if(ConnectionController.Instance.ConnectedAndOnline) {
                MakeRoomAvailableForRandomMatchMakingSearch();
                M3PUN.Utils.PrintRoomDetails("StateRandomMatchMakingHost", "DoWaitForBattlePartner");
                Helpers.Utility.LogMessage("Inside Lobby: " + PhotonNetwork.insideLobby + ((PhotonNetwork.lobby != null) ? PhotonNetwork.lobby.Name : "null"));
                Helpers.Utility.LogMessage("Room Name: " + PhotonNetwork.room.Name.ToString() + " prop: " + PhotonNetwork.room.CustomProperties.ToStringFull() + " pubpro:" + PhotonNetwork.room.CustomProperties.ToStringFull() + " LobbyName:" + PhotonNetwork.lobby + " open: " + PhotonNetwork.room.IsOpen + " vis:" + PhotonNetwork.room.IsVisible + " insideLobby: " + PhotonNetwork.insideLobby);
                VersusScreen.Instance.ShowCancelButtonWithCallback( MakeRoomAvailbleForChallengeAndGoBack, 0f);
                Invoke("DoSearchForPartnerInLobby", ServerConfigurableValues.Instance.TimeForBattlePartnerSearch);
            } else {
                DoConnect();
            }
        }
        void MakeRoomAvailableForRandomMatchMakingSearch() {
            DoUpdateRoomProperty(Utils.CTECH_PUN_ROOM_CHALLENGE_MODE, challengeMode);
            if(ConnectionController.Instance.inRoom && ConnectionController.Instance.room != null) {
                ConnectionController.Instance.room.IsOpen = true;
                //ConnectionController.Instance.room.IsVisible = true;
            }
        }

        void DoSearchForPartnerInLobby() {
            if(matchFound) {
                return;
            }
            CancelInvoke("DoSearchForPartnerInLobby");
            VersusScreen.Instance.HideCancelButton();
            ConnectionController.Instance.room.IsOpen = false;
            //ConnectionController.Instance.room.IsVisible = false;
            StateMachine.Instance.MakeTransition(typeof(StateRandomMatchMakingSearch));            
        }

        void MakeRoomAvailbleForChallengeAndGoBack() {
            CancelInvoke("DoSearchForPartnerInLobby");
            VersusScreen.Instance.HideCancelButton();
            DoUpdateRoomProperty(Utils.CTECH_PUN_ROOM_CHALLENGE_MODE, Utils.STR_PRIVATE);
            ConnectionController.Instance.room.IsOpen = true;
            //ConnectionController.Instance.room.IsVisible = true;
            VersusScreen.Instance.StopAnimation();
            StateMachine.Instance.MakeTransition(typeof(StateWaitForChallenge));
        }

        void DoConnect() {
            if(ConnectionController.Instance.ConnectedAndOnline) {
                //Photon is connected and it is not Offline. Using custom property otherwise PhotonNetwork.connected is true in offline mode as well
                _onConnectedToMaster();
            } else {
                //Either the game is disconnected or offline
                if(ConnectionController.Instance.connectionState != ConnectionState.Connecting) {
                    reconnecting = ConnectionController.Instance.ReconnectAndRejoin();
                    if(!reconnecting) {
                        if(!ConnectionController.Instance.Reconnect()) {
                            ConnectionController.Instance.Connect();
                        }
                    }
                }
            }
        }

        void DoMatchFoundGoForBattle() {
            VersusScreen.Instance.HideCancelButton();
            if(matchFound) {
                StateMachine.Instance.MakeTransition(typeof(StateGoForOnlineBattle));
            } else {
                DoSearchForPartnerInLobby();
            }
        }

        void DoOfflineBattle() {
            VersusScreen.Instance.HideCancelButton();
            StateMachine.Instance.MakeTransition(typeof(StateGoForAIBattle));
        }
        //Bound Callbacks
        protected override void _onConnectedToMaster() {
            if(reconnecting) {
                //reconnecting
            } else {
                DoInitNewRoomOptionsAndJoinRoom();
            }
            base._onConnectedToMaster();
        }

        void _onJoinedRoom() {
            M3PUN.Utils.PrintRoomDetails("StateRandomMatchMakingHost", "_onJoinedRoom");
            MakeRoomAvailableForRandomMatchMakingSearch();
            DoWaitForBattlePartner();
        }

        void _onJoinRoomFailed(object[] codeAndMsg) {
            Helpers.Utility.LogMessage("StateRandomMatchMakingHost _onJoinedRoomFailed: code:" + codeAndMsg[0].ToString() + " msg: " + codeAndMsg[1].ToString());
            string code = codeAndMsg[0].ToString();
            switch(code) {
                case M3Constants.PUN_ERROR_CODE_CCU_LIMIT_REACHED:
                    DoOfflineBattle();
                break;

                default:
                    DoInitNewRoomOptionsAndJoinRoom();
                break;

            }
        }

        void _onUpdateRoomProperties(object sender, Events.PhotonRoomPropertiesUpdate propset) {
            DoUpdateRoomProperty(propset);
        }

        void _onPhotonPlayerConnected (PhotonPlayer newPlayer)
		{
            CancelInvoke("DoSearchForPartnerInLobby");
            matchFound = true;
            VersusScreen.Instance.HideCancelButton();
            Helpers.Utility.LogMessage("StateRandomMatchMakingHost _onPhotonPlayerConnected: " + newPlayer.CustomProperties.ToString());

            DoMatchFoundGoForBattle();
		}
		void _onPhotonPlayerDisconnected (PhotonPlayer otherPlayer)
		{
            matchFound = false;
            ConnectionController.Instance.room.IsOpen = false;
            //ConnectionController.Instance.room.IsVisible = false;
			
            Helpers.Utility.LogMessage("StateRandomMatchMakingHost _onPhotonPlayerDisconnected: " + otherPlayer.CustomProperties.ToString());

            DoSearchForPartnerInLobby();			
		}

        // protected override void _onDisconnectedFromPhoton() {

        //     switch(disconnectionCause) {
        //         case DisconnectCause.ExceptionOnConnect:
        //         case DisconnectCause.Exception:
        //         case DisconnectCause.InvalidRegion:
        //         case DisconnectCause.SecurityExceptionOnConnect:
        //         DoOfflineBattle();
                
        //         break;

        //         case DisconnectCause.DisconnectByServerUserLimit:
        //         case DisconnectCause.MaxCcuReached:
        //         //Go Offline
        //         DoOfflineBattle();
        //         break;

        //         case DisconnectCause.AuthenticationTicketExpired:
        //         case DisconnectCause.InvalidAuthentication:
        //         DoOfflineBattle();
        //         break;

        //         case DisconnectCause.DisconnectByServerTimeout:
        //         case DisconnectCause.DisconnectByServerLogic:
        //         case DisconnectCause.DisconnectByClientTimeout:
        //         case DisconnectCause.InternalReceiveException:
        //         default:
        //         DoOfflineBattle();
        //         break;

        //         }
        // }
    }
}