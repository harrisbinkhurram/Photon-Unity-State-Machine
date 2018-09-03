using M3PUN;
using System.Collections;
using System.Collections.Generic;

namespace M3PUN {
    public class StateOnline : StateOnlineBase {
        bool findBattlePartner = false;

        public override void OnStateEnter() {
            base.OnStateEnter();
            Events.EventManager.Instance.FindBattlePartnerHandler += _OnFindBattlePartner;
            DoConnect();
        }
        
        public override void OnStateExit() {
            Events.EventManager.Instance.FindBattlePartnerHandler -= _OnFindBattlePartner;
            base.OnStateExit();
        }

        void DoConnect() {
            if(ConnectionController.Instance.ConnectedAndOnline) {
                //Photon is connected and it is not Offline. Using custom property otherwise PhotonNetwork.connected is true in offline mode as well
                _onConnectedToMaster();
            } else {
                //Either the game is disconnected or offline
                if(ConnectionController.Instance.Reconnect()) {

                } else {
                    ConnectionController.Instance.Connect(M3Constants.PHOTON_GAME_VERSION);
                }
            }
        }

        void _OnFindBattlePartner(object sender,System.EventArgs args) {
            findBattlePartner = true;

            VersusScreen.Instance.StartAnimation();
            M3GameCache.SetBattleType(BattleType.RandomPVP);            
        }

        //Bound Callbacks
        protected override void _onConnectedToMaster() {
            M3PUN.Utils.UpdatePhotonPlayerProperties();
            StateMachine.Instance.MakeTransition(typeof(StateWaitForChallenge), 
                        new Dictionary<string, object>() {
                        {StateWaitForChallenge.FIND_BATTLE_PARTNER, findBattlePartner }}
                    );
            base._onConnectedToMaster();
        }
    }
}
