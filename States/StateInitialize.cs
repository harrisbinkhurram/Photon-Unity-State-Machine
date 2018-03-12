using M3PUN;
using GemsFrontier;

namespace M3PUN {
    public class StateInitialize : StateBase {
        public override void OnStateEnter() {
            base.OnStateEnter();
            
            EventListenerForPrivateChallenge eventListener = GetComponent<EventListenerForPrivateChallenge>();
            if(eventListener == null) {
				eventListener = gameObject.AddComponent<EventListenerForPrivateChallenge>();
            }

            UserProfile up = GameState.Instance.Player.profile;

            ConnectionController.Instance.Initialize(up.UserId, up.Nick);
            if(ShallPlayOffline) {
                StateMachine.Instance.MakeTransition(typeof(StateOffline));
            } else {
                StateMachine.Instance.MakeTransition(typeof(StateOnline));
            }
            
        }

        public override void OnStateExit() {
            base.OnStateExit();
        }



        bool ShallPlayOffline {
            get {
                UserProfile up = GameState.Instance.Player.profile;
                if(up != null && up.Trophies < ServerConfigurableValues.Instance.TrophiesTillRealPVP || !TutorialController.HasTutorialEnded()) {
                    //Tutorial hasn't ended, or Trophies are less than the minimum required
                    return true;
                }
                return false;
            }
        }
    }
}