using GemsFrontier;
using UnityEngine;
using M3PUN;
using Photon;
using UnityEngine.SceneManagement;

namespace M3PUN {
    public class StateBattleEndCannotBattle : StateBase {
        bool messageShown = false;
        public override void OnStateEnter() {
            base.OnStateEnter();
            LoadingView.Instance.HoldTransition(true, false);
            ConnectionController.Instance.M3OnConnectedToMaster += _onConenctedToMaster;

            if(ConnectionController.Instance.inRoom) {
                ConnectionController.Instance.LeaveRoom();
            } else {
                _onConenctedToMaster();
            }
        }

        public override void OnStateExit() {
            ConnectionController.Instance.M3OnConnectedToMaster -= _onConenctedToMaster;
            UserPreferenceManager.SetAIBattleCompletionState (0);
            base.OnStateExit();
        }


        void _onConenctedToMaster() {
            if(messageShown) {
                return;
            }
            messageShown = true;

            StateMachine.Instance.MakeTransition(typeof(StateInitialize));
            ConnectionController.Instance.LoadLevelAsyncViaPhoton(SceneType.MainMenu.ToString());

            if(M3GameCache.GetIsFriendlyBattle()) {
                EventListenerForPrivateChallenge.ShowUserIsNotAvailablePopup(
                    LocalizationManager.Instance.GetString(LocalizationKeys.CANNOT_JOIN_BATTLE), ()=> {

                    }
                );
            } else {
                if(UserPreferenceManager.GetAIBattleCompletionState() == 0) {
                EventListenerForPrivateChallenge.ShowUserIsNotAvailablePopup(
                    LocalizationManager.Instance.GetString(LocalizationKeys.OTHER_PLAYER), ()=> {
                    }
                );
            }
        }
        }
    }
}