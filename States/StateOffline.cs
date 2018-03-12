using M3PUN;
using System.Collections.Generic;

namespace M3PUN {
    public class StateOffline : StateBase {
        bool findBattlePartner = false;
        public override void OnStateEnter() {
            base.OnStateEnter();
            M3GameCache.SetBattleType(BattleType.RandomPVP);
            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnect;
            ConnectionController.Instance.M3OnConnectedToMaster += _onConnect;

            Events.EventManager.Instance.MainMenuLoadCompleteHandler += _onMainMenuSceneLoaded;
            Events.EventManager.Instance.FindBattlePartnerHandler += _onFindBattlePartner;

            if(_data == null) {
                //no data was passed.
            } else {
                Dictionary<string, object> parameters = new Dictionary<string,object>();
                parameters = _data as Dictionary<string, object>;

                if(parameters.ContainsKey(StateWaitForChallenge.FIND_BATTLE_PARTNER)) {
                    findBattlePartner = (bool) parameters[StateWaitForChallenge.FIND_BATTLE_PARTNER];
                }
            }

            if(ConnectionController.Instance.ConnectedAndOnline) {
                ConnectionController.Instance.Disconnect(); //then wait for disconnect callback.
            } else if(PhotonNetwork.connecting) {
                //Wait for connect Callback
            } else {
                if(ConnectionController.Instance.offlineMode) {
                    _onConnect();
                } else {
                    ConnectionController.Instance.SwitchToOfflineMode();
                }
            }
            
        }

        public override void OnStateExit() {
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnect;
            ConnectionController.Instance.M3OnConnectedToMaster -= _onConnect;

            Events.EventManager.Instance.MainMenuLoadCompleteHandler -= _onMainMenuSceneLoaded;
            Events.EventManager.Instance.FindBattlePartnerHandler -= _onFindBattlePartner;

            base.OnStateExit();
        }

        void DoHideLoadingScreen() {
            if(LoadingView.Instance.IsVisible) {
                LoadingView.SharedInstance ().HideTransition();
            }
        }

        void DoUpdatePhotonPlayerProperties() {
            M3PUN.Utils.UpdatePhotonPlayerProperties();
        }

        void DoGoForAIBattle() {
            _onFindBattlePartner(null,null);
        }

        void _onFindBattlePartner(object sender,System.EventArgs args) {
            if(!VersusScreen.Instance.AnimContainer.isActiveAndEnabled) {
                VersusScreen.Instance.StartAnimation();
            }
            VersusScreen.Instance.HideCancelButton();
            StateMachine.Instance.MakeTransition(typeof(StateGoForAIBattle));
        }

        void _onDisconnect() {
            ConnectionController.Instance.SwitchToOfflineMode();
        }

        void _onConnect() {
            if(ConnectionController.Instance.ConnectedAndOnline) {
                ConnectionController.Instance.Disconnect();
            } else if (ConnectionController.Instance.offlineMode) {
                DoUpdatePhotonPlayerProperties();
                if(findBattlePartner) {
                    DoGoForAIBattle();
                }
            }
        }

        void _onMainMenuSceneLoaded(object sender, System.EventArgs args) {
            DoHideLoadingScreen();
            DoUpdatePhotonPlayerProperties();
        }
    }
}