using M3PUN;
using GemsFrontier;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace M3PUN {
    public class StateOnlineBattle : StateOnlineBattleBase {
        BattleField battleField;

        public override void OnStateEnter() {
            base.OnStateEnter();
            ConnectionController.Instance.M3OnPhotonPlayerConnected += _onPhotonPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected += _onPhotonPlayerDisconnected;
            ConnectionController.Instance.M3OnJoinedRoom += _onJoinedRoom;

            ConnectionController.Instance.M3OnConnectionFail += _onConnectionFail;
            ConnectionController.Instance.M3OnFailedToConnectToPhoton += _onConnectionFailedToConnect;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnectedFromPhoton;

            
            battleField = GameObject.FindGameObjectWithTag ("BattleField").GetComponent<BattleField> ();
        }

        public override void OnStateExit() {
            ConnectionController.Instance.M3OnPhotonPlayerConnected -= _onPhotonPlayerConnected;
            ConnectionController.Instance.M3OnPhotonPlayerDisconnected -= _onPhotonPlayerDisconnected;
            ConnectionController.Instance.M3OnJoinedRoom -= _onJoinedRoom;

            ConnectionController.Instance.M3OnConnectionFail -= _onConnectionFail;
            ConnectionController.Instance.M3OnFailedToConnectToPhoton -= _onConnectionFailedToConnect;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnectedFromPhoton;

            base.OnStateExit();
        }

        protected override void DoOfflineBattle() {
            if(battleField != null) {
                if(battleField.IsGameActive) {  //game has already begun.
                    return;
                } else if(battleField.IsGameEnd) { //game has already ended.
                    return;
                }
                //wait for player disconnected callback.
            }

            if(M3GameCache.GetIsFriendlyBattle()) {
                DoCannotDoBattle();
            } else {
                Helpers.Utility.LogMessage("StateOnlineBattle: Going for offlineBattle");
                Events.EventManager.Instance.BattleFieldSceneUnloadedHandler += onBattleFieldSceneUnloaded;
                if(SceneManager.GetActiveScene().name == SceneUtil.GetSceneName(SceneType.BattleField)) {
                    ConnectionController.Instance.LoadLevelAsyncViaPhoton(SceneUtil.GetSceneName (SceneType.BattleReloadScene));
                } else {
                    onBattleFieldSceneUnloaded(null,null);
                }
            }
        }

        void DoGoForAIBattle() {
            Helpers.Utility.LogMessage("StateOnlineBattle: DoGoForAIBattle");
            StateMachine.Instance.MakeTransition(typeof(StateGoForAIBattle));
        }

        void onBattleFieldSceneUnloaded(object sender, System.EventArgs args) {
            Helpers.Utility.LogMessage("StateOnlineBattle: onBattleFieldSceneUnloaded");
            Events.EventManager.Instance.BattleFieldSceneUnloadedHandler -= onBattleFieldSceneUnloaded;
            DoGoForAIBattle();
        }

        void DoCannotDoBattle() {
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
                        DoOfflineBattleAfterReload();
                    } else {
                        battleField.Invoke("WeDisconnected", 1f);
                    }
                } else {
                    DoOfflineBattleAfterReload();
                }
                //wait for player disconnected callback.
            } else {
                DoOfflineBattleAfterReload();
            }
        }


        protected void DoOfflineBattleAfterReload() {
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


        void _onPhotonPlayerConnected (PhotonPlayer newPlayer)
		{
            if(newPlayer.UserId != PhotonNetwork.player.UserId) {
                //other user rejoined after disconnect;
                CancelInvoke("DoOfflineBattle");
                battleField.CancelInvoke("OpponentDisconnected");
            }
		}

        void _onJoinedRoom() {
            CancelInvoke("DoOfflineBattle");
            battleField.CancelInvoke("WeDisconnected");
        }

		void _onPhotonPlayerDisconnected (PhotonPlayer otherPlayer)
		{   
            bool isOurPlayer = false;
            if(otherPlayer.UserId == PhotonNetwork.player.UserId) {
                isOurPlayer = true;
            }

            Helpers.Utility.LogMessage("StateOnlineBattle _onPhotonPlayerDisconnected: " + otherPlayer.CustomProperties.ToString());
            if(!battleField.IsGameActive && !battleField.IsGameEnd) {
                //disconnected before battle begins
                Helpers.Utility.LogMessage("StateOnlineBattle _onPhotonPlayerDisconnected: disconnected before battle begins" + otherPlayer.UserId);
                if(VersusScreen.Instance.AnimContainer.isActiveAndEnabled) {
                    VersusScreen.Instance.RestartAnimation();
                    DoOfflineBattle();
                } else {
                    battleField.Invoke("WeDisconnected", 1f);
                }
            } else if (battleField.IsGameActive && !battleField.IsGameEnd) {
                if(isOurPlayer) {
                    Helpers.Utility.LogMessage("StateOnlineBattle _onPhotonPlayerDisconnected: disc before battleEnd battleLost" + otherPlayer.UserId);
                    battleField.Invoke("WeDisconnected", 1f);
                } else {
                    Helpers.Utility.LogMessage("StateOnlineBattle _onPhotonPlayerDisconnected: disc before battleEnd battleWon" + otherPlayer.UserId);
                    battleField.Invoke("OpponentDisconnected", 1f);
                }
            } else if (battleField.IsGameEnd) {
                Helpers.Utility.LogMessage("StateOnlineBattle _onPhotonPlayerDisconnected: disc before after battleEnd" + otherPlayer.UserId);
                //ignore because on battle end this state shouldn't be up
            } else {
                Helpers.Utility.LogMessage("StateOnlineBattle _onPhotonPlayerDisconnected: ELSE DoOFFLINEBATTLE" + otherPlayer.UserId);
                DoOfflineBattle();
            }
		}

        protected override void tryToReconnect() {
            if(reconnectRetryCount > MAX_RETRIES) {
                if(LoadingView.Instance.IsVisible) {
                    LoadingView.Instance.HideTransition();
                }
                if(VersusScreen.Instance.AnimContainer.isActiveAndEnabled) {
                    VersusScreen.Instance.StopAnimation();
                }
                if(battleField != null && battleField.IsGameActive && !battleField.IsGameEnd) {
                    _onPhotonPlayerConnected(PhotonNetwork.player);
                } else {
                    DoOfflineBattle();
                }
            } else {
                reconnecting = ConnectionController.Instance.ReconnectAndRejoin();
                if(!reconnecting) {
                    if(!ConnectionController.Instance.Reconnect()) {
                        ConnectionController.Instance.Connect();
                    }
                }
            }

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
                DoCannotDoBattle();
                break;

                }
        }

        protected override void _onConnectionFail(DisconnectCause cause) {    
            Helpers.Utility.LogMessage("_onConnectionFail: " + cause.ToString() + " Class: " +  this.GetType().Name);            
            disconnectionCause = cause;
            reconnectRetryCount++;
        }
        
        protected override void _onConnectionFailedToConnect(DisconnectCause cause) {
            Helpers.Utility.LogError("_onConnectionFailedToConnect: " + cause.ToString() + " Class: " +  this.GetType().Name);            
            disconnectionCause = cause;
            reconnectRetryCount++;
            _onDisconnectedFromPhoton();
        }
    }
}