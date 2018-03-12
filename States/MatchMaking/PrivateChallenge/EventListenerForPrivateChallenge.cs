using UnityEngine;
using Photon;
using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GemsFrontier;
using M3PUN;

namespace M3PUN {
    public class EventListenerForPrivateChallenge : Photon.PunBehaviour {
        bool linked = false;
        void OnEnable() {
            if(linked) {
                return;
            }
            linked = true;
            ConnectionController.Instance.OnEventCallHandler += OnPhotonEvent;
        }
        void OnDisable() {
            if(linked) {
                ConnectionController.Instance.OnEventCallHandler -= OnPhotonEvent;
                linked = false;
            }
        }

        public void OnPhotonEvent(byte eventcode, object content, int senderid) {
            Helpers.Utility.LogMessage("HK ||||| OnPhotonEvent: " + eventcode.ToString() +" " + eventcode);

            if((int) eventcode <= (int)PUNFriendlyBattleEvents.NONE || (int) eventcode >= (int)PUNFriendlyBattleEvents.MAX) {
                return;
            }

            byte[] dataArr = (byte[])content;
            BattleType battleType = (BattleType)(int)dataArr[0];

            PUNFriendlyBattleEvents evt = ((PUNFriendlyBattleEvents)((byte)eventcode));

            bool OwnSide = (senderid == PhotonNetwork.player.ID);

            Helpers.Utility.LogMessage("Event Listener ---> Photon Event:" + evt.ToString() + " senderId: " + senderid.ToString() + " photonId: " + PhotonNetwork.player.ID);                

            switch(evt) {
                case PUNFriendlyBattleEvents.CHALLENGE_SEND:
                    Events.EventManager.Instance.PrivateChallengeMessage(PhotonNetwork.player, PhotonNetwork.otherPlayers, battleType);
                break;
                case PUNFriendlyBattleEvents.CHALLENGE_YES:
                    Events.EventManager.Instance.PrivateChallengeYes(PhotonNetwork.player, PhotonNetwork.otherPlayers, battleType);
                break;
                case PUNFriendlyBattleEvents.CHALLENGE_IGNORE:
                    Events.EventManager.Instance.PrivateChallengeIgnore(PhotonNetwork.player, PhotonNetwork.otherPlayers, battleType);
                break;

                case PUNFriendlyBattleEvents.CHALLENGE_NO:
                    Events.EventManager.Instance.PrivateChallengeNo(PhotonNetwork.player, PhotonNetwork.otherPlayers, battleType);
                break;

                case PUNFriendlyBattleEvents.BATTLE_START:
                    Events.EventManager.Instance.PrivateChallengeStartBattle(PhotonNetwork.player, PhotonNetwork.otherPlayers, battleType);
                break;

                case PUNFriendlyBattleEvents.CHALLENGE_BUSY:
                    Events.EventManager.Instance.PrivateChallengeBusy(PhotonNetwork.player, PhotonNetwork.otherPlayers, battleType);
                break;
            }
        }

        public void SendFriendlyBattleChallenge(BattleType battleType) {
            byte evCode = (byte)PUNFriendlyBattleEvents.CHALLENGE_SEND;
            byte[] content = new byte[] { (byte)battleType };
            bool reliable = true;
            RaiseEventOptions options = new RaiseEventOptions();
            options.Receivers = ReceiverGroup.All;        
            PhotonNetwork.RaiseEvent(evCode, content, reliable, options);
        }

        public void SendFriendlyBattleChallengeYES(BattleType battleType) {
            Debug.Log("SendFriendlyBattleChallengeYES");
            byte evCode = (byte)PUNFriendlyBattleEvents.CHALLENGE_YES;
            byte[] content = new byte[] { (byte)battleType };
            bool reliable = true;
            RaiseEventOptions options = new RaiseEventOptions();
            options.Receivers = ReceiverGroup.All;        

            PhotonNetwork.RaiseEvent(evCode, content, reliable, options);

        }


        public void SendFriendlyBattleChallengeNO(BattleType battleType) {
            Debug.Log("SendFriendlyBattleChallengeNo");
            byte evCode = (byte)PUNFriendlyBattleEvents.CHALLENGE_NO;
            byte[] content = new byte[] { (byte)battleType };
            bool reliable = true;
            RaiseEventOptions options = new RaiseEventOptions();
            options.Receivers = ReceiverGroup.All;        

            PhotonNetwork.RaiseEvent(evCode, content, reliable, options);

            
        }

        public void SendFriendlyBattleChallengeIgnore(BattleType battleType) {
            byte evCode = (byte)PUNFriendlyBattleEvents.CHALLENGE_IGNORE;
            byte[] content = new byte[] { (byte)battleType };
            bool reliable = true;
            RaiseEventOptions options = new RaiseEventOptions();
            options.Receivers = ReceiverGroup.All;
            
            PhotonNetwork.RaiseEvent(evCode, content, reliable, options);
            
            
        }
        
        public void SendFriendlyBattleChallengeBUSY(BattleType battleType) {
            byte evCode = (byte)PUNFriendlyBattleEvents.CHALLENGE_BUSY;
            byte[] content = new byte[] { (byte)battleType };
            bool reliable = true;
            RaiseEventOptions options = new RaiseEventOptions();
            options.Receivers = ReceiverGroup.All;        

            PhotonNetwork.RaiseEvent(evCode, content, reliable, options);
        }

        public void SendFriendlyBattleStartSignal(BattleType battleType) {
            byte evCode = (byte)PUNFriendlyBattleEvents.BATTLE_START;
            byte[] content = new byte[] { (byte)battleType };
            bool reliable = true;
            RaiseEventOptions options = new RaiseEventOptions();
            options.Receivers = ReceiverGroup.All;        

            PhotonNetwork.RaiseEvent(evCode, content, reliable, options);
        }


//TODO
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
    //FOLLOWING METHODS SHOULD BE IMPLEMENTED ELSEWHERE
//TODO

//STATICS

        public static AlertPopup ShowChallengePopup(string title, string message, Action callbackYes, Action callbackNo){
           
            AlertPopup alert = null;

            Action OnAcceptButtonAction = ()=> {
                callbackYes();
                alert.ScheduleCloseButtonPressed(-1);
                M3Utils.Instance.CloseAllPopups();
            };

            Action OnDeclineButtonAction = ()=> {
                callbackNo();
                alert.ScheduleCloseButtonPressed(-1);
                alert.HideAndDestroy();
            };

            alert = AlertPopup.GetAlertPopup(AlertPopup.PopupType.Normal,
            title,
            message,
            LocalizationManager.Instance.GetString(LocalizationKeys.ACCEPT),
            LocalizationManager.Instance.GetString(LocalizationKeys.DECLINE),
            OnAcceptButtonAction,
            OnDeclineButtonAction,
            OnDeclineButtonAction,
            true, true);

            alert.ScheduleCloseButtonPressed(Mathf.Clamp(ServerConfigurableValues.Instance.BattleChallengeRoomWait, 10, ServerConfigurableValues.Instance.LoadingFailureTime - 5));
            alert.Show();
            return alert;
        }

        public static AlertPopup ShowDisconnectedNoInternetConnectivity(Action callback = null) {
            string title = LocalizationManager.Instance.GetString(LocalizationKeys.DISCONNECTED);
            string message = LocalizationManager.Instance.GetString(LocalizationKeys.NO_INTERNET_CONNECTIVITY);
            return ShowPopupWithMessageAndCallback(title, message, callback);
        }
        
        public static AlertPopup ShowDisconnectedFromRoom(Action callback = null) {
            string title = LocalizationManager.Instance.GetString(LocalizationKeys.CANNOT_JOIN);
            string message = LocalizationManager.Instance.GetString(LocalizationKeys.CONNECTION_IN_PROGRESS);
            return ShowPopupWithMessageAndCallback(title, message, callback);
        }

        public static AlertPopup ShowCannotChallengeSelfPopup(string userNick, Action callback = null) {
            string title = LocalizationManager.Instance.GetString(LocalizationKeys.CANNOT_JOIN);
            string message = LocalizationManager.Instance.GetString(LocalizationKeys.CANNOT_CHALLENGE_SELF);
            return ShowPopupWithMessageAndCallback(title, message, callback);
        }

        public static AlertPopup ShowUserIsNotAvailablePopup(string userNick, Action callback = null) {
            string title = LocalizationManager.Instance.GetString(LocalizationKeys.CANNOT_JOIN);
            string message = string.Format(LocalizationManager.Instance.GetString(LocalizationKeys.THIS_USER_OFFLINE), userNick);
            return ShowPopupWithMessageAndCallback(title, message, callback);
        }

        public static AlertPopup ShowUserIsBusyPopup(string userNick, Action callback = null) {
            string title = LocalizationManager.Instance.GetString(LocalizationKeys.CANNOT_JOIN);
            string message = string.Format(LocalizationManager.Instance.GetString(LocalizationKeys.USER_CHALLENGE_BUSY), userNick);
            return ShowPopupWithMessageAndCallback(title, message, callback);
        }

        public static AlertPopup ShowUserDeclinedPopup(string userNick, Action callback = null) {
            string title = LocalizationManager.Instance.GetString(LocalizationKeys.CHALLENGE_DECLINED);
            string message = string.Format(LocalizationManager.Instance.GetString(LocalizationKeys.USER_DECLINED_CHALLENGE), userNick);
            return ShowPopupWithMessageAndCallback(title, message, callback);
        }

        public static AlertPopup ShowPopupWithMessageAndCallback(string title, string message, Action callback = null) {
            AlertPopup alert = null;

            Action OnOkayButtonAction = ()=> {
                if(callback != null) {
                    callback();
                }
                alert.HideAndDestroy();
            };

            Action OnCloseButtonAction = OnOkayButtonAction;

            alert = AlertPopup.GetAlertPopup(AlertPopup.PopupType.Critical,
            title,
            message,
            LocalizationManager.Instance.GetString(LocalizationKeys.OK),
            OnOkayButtonAction,
            OnCloseButtonAction,
            true, true);
            alert.Show();
            return alert;
        }
    }
}