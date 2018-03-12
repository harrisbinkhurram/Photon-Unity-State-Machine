using System.Collections;
using UnityEngine;
namespace M3PUN {
    public class StateBase : MonoBehaviour  {
        protected object _data;
        public virtual void OnStateEnter() {
            if(UserPrefHelper.IsLogEnabled) {
                Helpers.Utility.LogMessage("PhotonStateMachine: OnStateEnter: " +  this.GetType().Name);            
            }
        }

        public virtual void OnStateExit() {
            if(UserPrefHelper.IsLogEnabled) {
                Helpers.Utility.LogMessage("PhotonStateMachine: OnStateExit: " +  this.GetType().Name);
            }
            _data = null;
            StopAllCoroutines();
            CancelInvoke();
            Destroy(this);   
        }

        public virtual void SetData(object data) {
            _data = data;
        }
    }
}


/*
    Dedicated to my beloved wife Wajeeha and our daughter Aqsa for bearing up with me,
    and letting me re-write this thing 24/7 staying up all day and all night.
    I wouldn't have made it without you two on my Side.

    now here's the theme song for our First Global Release and the Photon Problem:
    
    https://www.youtube.com/watch?v=h9QNUcrjtOs

    Thanks to the singer, that was totally me when we ran into problems.

*/
