using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace M3PUN {
    public class StateMachine : MonoBehaviour {
        private static StateMachine _instance;
		private static object _lock = new object();
		private static bool applicationIsQuitting = false;
		public static StateMachine Instance
		{
			get
			{
				if (applicationIsQuitting) {
					Debug.LogWarning("[Singleton] Instance '"+ typeof(StateMachine) +
						"' already destroyed on application quit." +
						" Won't create again - returning null.");
					return null;
				}

				lock(_lock)
				{
					if (_instance == null)
					{
						_instance = (StateMachine) FindObjectOfType(typeof(StateMachine));

						if ( FindObjectsOfType(typeof(StateMachine)).Length > 1 )
						{
							Debug.LogError("[Singleton] Something went really wrong " +
								" - there should never be more than 1 singleton!" +
								" Reopening the scene might fix it.");
							return _instance;
						}

						if (_instance == null)
						{
							GameObject singleton = new GameObject();
							_instance = singleton.AddComponent<StateMachine>();
							singleton.name = "(singleton) "+ typeof(StateMachine).ToString();

							DontDestroyOnLoad(singleton);

							Debug.Log("[Singleton] An instance of " + typeof(StateMachine) + 
								" is needed in the scene, so '" + singleton +
								"' was created with DontDestroyOnLoad.");
						} else {
							Debug.Log("[Singleton] Using instance already created: " +
								_instance.gameObject.name);
						}
					}

					return _instance;
				}
			}
		}

        public StateBase CurrentState;
		void OnEnable() {
			M3Utils.Instance.GameRestarted += OnGameRestart;
		}

		void OnDisable() {
			M3Utils.Instance.GameRestarted -= OnGameRestart;
		}

        public void MakeTransition(Type newState, object DataToPass=null) {
            if(CurrentState != null) {
                CurrentState.OnStateExit();
            }

            CurrentState = (StateBase) gameObject.AddComponent(newState);
			
			if(DataToPass != null) {
				CurrentState.SetData(DataToPass);
			}
			
            CurrentState.OnStateEnter();
        }

		void OnGameRestart(object sender, EventArgs args) {
			if(CurrentState != null) {
                CurrentState.OnStateExit();
            }
		}
    }
}