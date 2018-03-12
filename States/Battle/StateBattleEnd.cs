using GemsFrontier;
using UnityEngine;
using M3PUN;
using Photon;

namespace M3PUN {
    public class StateBattleEnd : StateBase {
        
        public override void OnStateEnter() {
            base.OnStateEnter();
            ConnectionController.Instance.M3OnLeftRoom += _onLeftRoom;
            if(ConnectionController.Instance.inRoom) {
                ConnectionController.Instance.LeaveRoom();
            } else {
                _onLeftRoom();
            }
        }

        public override void OnStateExit() {
            ConnectionController.Instance.M3OnLeftRoom -= _onLeftRoom;
            base.OnStateExit();
        }

        void _onLeftRoom() {
            StateMachine.Instance.MakeTransition(typeof(StateInitialize));
        }
    }    
}