using M3PUN;
using GemsFrontier;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace M3PUN {
    public class StateGoForOnlineBattle : StateOnlineBattleBase {
        BattleField battleField;

        public override void OnStateEnter() {
            base.OnStateEnter();
            ConnectionController.Instance.M3OnPhotonPlayerConnected += _onPhotonPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected += _onPhotonPlayerDisconnected;
            
            Events.EventManager.Instance.LoadBattleFieldSceneEventHandler += _onLoadBattleFieldScene;

            ConnectionController.Instance.M3OnConnectionFail += _onConnectionFail;
            ConnectionController.Instance.M3OnFailedToConnectToPhoton += _onConnectionFailedToConnect;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnectedFromPhoton;


            if(ConnectionController.Instance.room.PlayerCount >= Utils.CTECH_PUN_MAX_PLAYERS) {
                DoOnlineBattle();
            } else {
                DoOfflineBattle();
            }
        }

        public override void OnStateExit() {

            ConnectionController.Instance.M3OnPhotonPlayerConnected -= _onPhotonPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected -= _onPhotonPlayerDisconnected;

            Events.EventManager.Instance.LoadBattleFieldSceneEventHandler -= _onLoadBattleFieldScene;

            ConnectionController.Instance.M3OnConnectionFail -= _onConnectionFail;
            ConnectionController.Instance.M3OnFailedToConnectToPhoton -= _onConnectionFailedToConnect;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnectedFromPhoton;


            base.OnStateExit();
        }

        void DoOnlineBattle() {
            Helpers.Utility.LogMessage("StateGoForOnlineBattle: DoOnlineBattle");

            if(ConnectionController.Instance.isMasterClient) {
                Helpers.Utility.LogMessage("StateGoForOnlineBattle: DoOnlineBattle MasterClient");
                /*
                    //enabling following properties to be set overflows sdk buffers an causes a disconnect.
                    
                    // string[] expectedUsers = { PhotonNetwork.player.UserId, PhotonNetwork.otherPlayers[0].UserId };
                    // ConnectionController.Instance.room.SetExpectedUsers( expectedUsers );   //setting expected users so these users could rejoin the closed room.
                    //ConnectionController.Instance.room.IsVisible= false;
                */

                ConnectionController.Instance.room.IsOpen = false;
                ConnectionController.Instance.SendOutgoingCommands();
            }

            VersusScreen.Instance.SetVersusScreenOpponentPlayerProperties (PhotonNetwork.otherPlayers[0]);
        }
        
        protected override void DoOfflineBattle() {
            if(M3GameCache.GetIsFriendlyBattle()) {
                VersusScreen.Instance.StopAnimation(); 
                StateMachine.Instance.MakeTransition(typeof(StateBattleEndCannotBattle));
            } else {
                Helpers.Utility.LogMessage("StateGoForOnlineBattle: Going for offlineBattle");
                VersusScreen.Instance.RestartAnimation();
                Events.EventManager.Instance.BattleFieldSceneUnloadedHandler += onBattleFieldSceneUnloaded;
                if(SceneManager.GetActiveScene().name == SceneUtil.GetSceneName(SceneType.BattleField)) {
                    ConnectionController.Instance.LoadLevelAsyncViaPhoton(SceneUtil.GetSceneName (SceneType.BattleReloadScene));
                } else {
                    onBattleFieldSceneUnloaded(null,null);
                }
            }
        }
        void DoCannotDoBattle() {
            GameObject gObj = GameObject.FindGameObjectWithTag ("BattleField");
            
            if(gObj != null) {
                battleField = gObj.GetComponent<BattleField> ();
            }

            if(battleField != null) {
                if(battleField.IsGameActive && !battleField.IsGameEnd) {  //game has already begun.
                    battleField.WeDisconnected();
                } else if(battleField.IsGameEnd) { //game has already ended.
                    StateMachine.Instance.MakeTransition(typeof(StateBattleEnd));
                } else if(!battleField.IsGameActive && !battleField.IsGameEnd) {
                    //disconnected before battle begins
                    Helpers.Utility.LogMessage("StateOnlineBattle DoCannotDoBattle: disconnected before battle begins");
                    if(VersusScreen.Instance.AnimContainer.isActiveAndEnabled) {
                        VersusScreen.Instance.RestartAnimation();
                        DoOfflineBattle();
                    } else {
                        battleField.Invoke("WeDisconnected", 1f);
                    }
                } else {
                    DoOfflineBattle();
                }
                //wait for player disconnected callback.
            } else {
                DoOfflineBattle();
            }
        }

        void DoGoForAIBattle() {
            Helpers.Utility.LogMessage("StateGoForOnlineBattle: DoGoForAIBattle");
            StateMachine.Instance.MakeTransition(typeof(StateGoForAIBattle));
        }

        void onBattleFieldSceneUnloaded(object sender, System.EventArgs args) {
            Helpers.Utility.LogMessage("StateGoForOnlineBattle: onBattleFieldSceneUnloaded");
            Events.EventManager.Instance.BattleFieldSceneUnloadedHandler -= onBattleFieldSceneUnloaded;
            DoGoForAIBattle();
        }

        void _onLoadBattleFieldScene(object sender, System.EventArgs args) {
            ConnectionController.Instance.LoadLevelAsyncViaPhoton(SceneUtil.GetSceneName (SceneType.BattleField));
        }

        void _onPhotonPlayerConnected (PhotonPlayer newPlayer)
		{
            if(newPlayer.UserId != PhotonNetwork.player.UserId) {
                //other user rejoined after disconnect;
                CancelInvoke("DoOfflineBattle");
            }
		}
		void _onPhotonPlayerDisconnected (PhotonPlayer otherPlayer)
		{
            Helpers.Utility.LogMessage("StateGoForOnlineBattle _onPhotonPlayerDisconnected: " + otherPlayer.CustomProperties.ToString());
            DoOfflineBattle();
		}

         protected override void _onDisconnectedFromPhoton() {
            switch(disconnectionCause) {
                case DisconnectCause.ExceptionOnConnect:
                case DisconnectCause.Exception:
                case DisconnectCause.InvalidRegion:
                case DisconnectCause.SecurityExceptionOnConnect:
                //Show Error Try to Reconnect
                disconnectionCause = 0;
                DoCannotDoBattle();
                break;

                case DisconnectCause.DisconnectByServerUserLimit:
                case DisconnectCause.MaxCcuReached:
                //Go Offline
                disconnectionCause = 0;
                DoCannotDoBattle();
                break;

                case DisconnectCause.AuthenticationTicketExpired:
                case DisconnectCause.InvalidAuthentication:
                disconnectionCause = 0;
                DoCannotDoBattle();
                break;

                case DisconnectCause.DisconnectByServerTimeout:
                case DisconnectCause.DisconnectByServerLogic:
                case DisconnectCause.DisconnectByClientTimeout:
                case DisconnectCause.InternalReceiveException:
                default:
                if(reconnectRetryCount < MAX_RETRIES) {
                    reconnectRetryCount++;
                    if(ConnectionController.Instance.connectionState == ConnectionState.Connecting) {

                    } else {
                        base._onDisconnectedFromPhoton();
                    }
                    //DoCannotDoBattle();
                } else {
                    DoCannotDoBattle();
                }
                
                break;

                }
        }

        protected override void _onConnectionFail(DisconnectCause cause) {    
            Helpers.Utility.LogMessage("_onConnectionFail: " + cause.ToString() + " Class: " +  this.GetType().Name);            
            disconnectionCause = cause;
        }
        
        protected override void _onConnectionFailedToConnect(DisconnectCause cause) {
            Helpers.Utility.LogError("_onConnectionFailedToConnect: " + cause.ToString() + " Class: " +  this.GetType().Name);            
            disconnectionCause = cause;
            _onDisconnectedFromPhoton();
        }

    }
}