using M3PUN;
using Photon;
using GemsFrontier;

namespace M3PUN {
    public class StateReceivedChallengeGuild : StateReceivedChallengeBase {

        public override void OnStateEnter() {
            base.OnStateEnter();
            string title = LocalizationManager.Instance.GetString(LocalizationKeys.CHALLENGE);
            string message = string.Format(LocalizationManager.Instance.GetString(LocalizationKeys.USER_CHALLENGED_GUILD), privateChallengeArgs.OpponentNickName);
            popup = EventListenerForPrivateChallenge.ShowChallengePopup(title, message, DoUserSaidYes, DoUserSaidNo);
        }
        
        public override void OnStateExit() {
            base.OnStateExit();
        }

    }
}