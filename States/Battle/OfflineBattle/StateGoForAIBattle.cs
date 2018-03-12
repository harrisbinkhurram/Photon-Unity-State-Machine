using M3PUN;
using GemsFrontier;
using UnityEngine.SceneManagement;

namespace M3PUN {
    public class StateGoForAIBattle : StateBase {
        bool doingOfflineBattle = false;

        public override void OnStateEnter() {
            base.OnStateEnter();
            HeroBrigade.PickM3OSParamsInAdvance();

            ConnectionController.Instance.M3OnConnectedToMaster += _onConnectedToMaster;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onDisconnectedFromPhoton;
            
            Events.EventManager.Instance.LoadBattleFieldSceneEventHandler += _onLoadBattleFieldScene;
            if(!VersusScreen.Instance.AnimContainer.isActiveAndEnabled) {
                VersusScreen.Instance.StartAnimation();
            }

            if(ConnectionController.Instance.ConnectedAndOnline) {
                ConnectionController.Instance.Disconnect();
            } else if (!ConnectionController.Instance.connected && !ConnectionController.Instance.offlineMode) {
                //not connected and But not offline
                _onDisconnectedFromPhoton();
            } else {
                DoOfflineBattle();
            }
        }

        public override void OnStateExit() {
            ConnectionController.Instance.M3OnConnectedToMaster -= _onConnectedToMaster;
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onDisconnectedFromPhoton;

            Events.EventManager.Instance.LoadBattleFieldSceneEventHandler -= _onLoadBattleFieldScene;

            base.OnStateExit();
        }

        void DoOfflineBattle() {
            if(doingOfflineBattle) {
                return;
            }
            
            doingOfflineBattle = true;

            string nickName = M3GameCache.GetAINickName ();
            int leaderId = M3GameCache.GetAILeaderCardId ();
            int trophies = M3GameCache.GetAITrophies();
            int arenaLevel = M3GameCache.GetAIArenaLevel();
            int userLevel = M3GameCache.GetAIUserLevel();
            string guildName = M3GameCache.GetAIGuildName();
            VersusScreen.Instance.SetOpponentAttribs (nickName, string.Empty, leaderId, trophies, arenaLevel, userLevel, guildName);
            UserPreferenceManager.SetAIBattleCompletionState (1);
        }

        void _onLoadBattleFieldScene(object sender, System.EventArgs args) {
            if(ConnectionController.Instance.isMasterClient) {
                ConnectionController.Instance.LoadLevelAsyncViaPhoton (SceneUtil.GetSceneName (SceneType.BattleField));
            }
        }
        void _onConnectedToMaster() {
            if(ConnectionController.Instance.ConnectedAndOnline) {
                ConnectionController.Instance.Disconnect();
            } else {
                DoOfflineBattle();
            }
        }

        void _onDisconnectedFromPhoton() {
            ConnectionController.Instance.SwitchToOfflineMode();
        }
    }
}