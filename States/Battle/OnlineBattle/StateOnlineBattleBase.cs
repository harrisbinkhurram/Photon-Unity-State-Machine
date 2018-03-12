using System.Collections;
using Photon;
using UnityEngine;
using GemsFrontier;

namespace M3PUN {
    public class StateOnlineBattleBase : StateOnlineBase  {

        protected virtual void DoOfflineBattle() {
            //ovrride in derived class
        }

        protected override void _onDisconnectedFromPhoton() {
            if(reconnectRetryCount < MAX_RETRIES) {
                base._onDisconnectedFromPhoton();
            } else {
                DoOfflineBattle();
            }
        }
    }
}