using GemsFrontier;
using UnityEngine;
using M3PUN;
using Photon;
using UnityEngine.SceneManagement;

namespace M3PUN {
    public class StateResetEverything : StateBase {
        
        public override void OnStateEnter() {
            base.OnStateEnter();

            ConnectionController.Instance.M3OnDisconnectedFromPhoton += _onOnDisconnectedFromPhoton;

            if(ConnectionController.Instance.connected) {
                ConnectionController.Instance.Disconnect();
            } else {
                _onOnDisconnectedFromPhoton();
            }
        }

        public override void OnStateExit() {
            base.OnStateExit();
            ConnectionController.Instance.M3OnDisconnectedFromPhoton -= _onOnDisconnectedFromPhoton;
        }


        void _onOnDisconnectedFromPhoton() {
            if(VersusScreen.Instance.AnimContainer.isActiveAndEnabled) {
                VersusScreen.Instance.EndAnimation();
            }
            LoadingView.Instance.HoldTransition(true, false);

            StateMachine.Instance.MakeTransition(typeof(StateInitialize));
            ConnectionController.Instance.LoadLevelAsyncViaPhoton(SceneType.MainMenu.ToString());
            EventListenerForPrivateChallenge.ShowPopupWithMessageAndCallback(LocalizationManager.Instance.GetString(LocalizationKeys.ERROR), LocalizationManager.Instance.GetString(LocalizationKeys.ERROR_CONNECTING_PLEASE_TRY_AGAIN));
        }

    }    
}