using PVR.CCK.Worlds.Components;
using PVR.PSharp;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PSharpVideo
{
	public class PSharpVideoPlayer : PSharpBehaviour
	{
		[Header("Config")]
		
		[PSharpSynced(SyncType.Automatic, nameof(OnMasterLockChanged))] public bool isMasterLocked;
		[PSharpSynced(SyncType.Automatic)] public string video;
		[PSharpSynced(SyncType.Automatic, nameof(OnPlaybackChanged))] public float playbackRate = 1f;

		[Space]
		[Space]

		public PVR_AVProVideoProvider videoProvider;

		[Space]
		[Space]

		[Header("User Interface")]
		public TMP_InputField urlInput;

		public TMP_InputField currentVideo;
		public TMP_InputField lastVideo;

		[Space]
		
		public Button infoButton;
		public Button reloadButton;
		public Button loopButton;
		public Button masterLockButton;
		public Button muteButton;
		public Button pauseResumeButton;
		public Slider timelineSlider;
		public Slider playbackSlider;

		[Space]
		
		public GameObject[] masterLockIcons;
		public GameObject[] muteIcons;
		public GameObject[] pauseResumeIcons;
		public GameObject[] infoObjects;

		[Space]

		public TMP_Text statusText;
		public TMP_Text masterName;
		public TMP_Text videoOwner;

		[Space]

		public AudioSource[] speakers;


		public Graphic loopButtonIcon;

		public Graphic loopButtonBackground;

		private string _videoLength;
		private string _masterName;
		private bool _isMuted = false;

		[PSharpSynced(SyncType.Automatic, nameof(OnVideoOwnerChanged))] private string _videoOwnerName;
		[PSharpSynced(SyncType.Automatic, nameof(OnLastVideoChanged))] private string _lastVideo;
		[PSharpSynced(SyncType.Automatic, nameof(OnURLChanged))] private string _currentUrl;
		[PSharpSynced(SyncType.Automatic, nameof(OnTimeChanged))] private double _time;
		[PSharpSynced(SyncType.Automatic, nameof(OnPlayPauseChanged))] private bool _isPaused;
		[PSharpSynced(SyncType.Automatic, nameof(OnLoopChanged))] private bool _isLooping;

		bool newJoinPause;

		[Space]
		[Header("Style Colors")]
		
		public Color buttonActivatedColor = new Color(1f, 1f, 1f, 1f);
		public Color iconInvertedColor = new Color(1f, 1f, 1f, 1f);
		public Color whiteGraphicColor = new Color(0.9433f, 0.9433f, 0.9433f);
		public Color buttonBackgroundColor = new Color(1f, 1f, 1f, 1f);

		private void OnPlaybackChanged()
		{
            if (!videoProvider.isLiveStream)
            {
                Debug.Log($"Playback rate changed, new value is {playbackRate}");
				videoProvider.PlaybackRate = playbackRate;
            }
			else
			{
				videoProvider.PlaybackRate = 1;
			}
            
		}

        private void OnURLChanged() 
		{
			Debug.Log($"URL Changed: {_currentUrl}");
			if (!string.IsNullOrWhiteSpace(_currentUrl))
			{
				currentVideo.text = _currentUrl;
				videoProvider.PlayURL(_currentUrl);
			}
		}

		private void OnTimeChanged()
		{
			
			if (videoProvider.isLoading || IsOwner || videoProvider.isLiveStream)
				return;

			float desync = (float)_time - (float)videoProvider.Time;
	
			if (Mathf.Abs(desync) > 4)
			{
				videoProvider.Time = _time;
				if (videoProvider.IsPaused && !_isPaused) videoProvider.IsPaused = false;
			}
		}

		private void OnVideoOwnerChanged()
		{
			Debug.Log("Video Owner Changed");
			videoOwner.text = _videoOwnerName;
		}

		private void OnLastVideoChanged()
		{
			Debug.Log("Last Video Changed");

			lastVideo.text = _lastVideo;
		}

		private void OnPlayPauseChanged()
		{
			Debug.Log("PlayPause Changed");
			if(_isPaused)
			{
				pauseResumeIcons[0].SetActive(true);
				pauseResumeIcons[1].SetActive(false);
				videoProvider.IsPaused = true;
			}
			else 
			{
				pauseResumeIcons[0].SetActive(false);
				pauseResumeIcons[1].SetActive(true);
				videoProvider.IsPaused = false;
			}
		}

		private void OnMasterLockChanged()
		{
			Debug.Log("Master Lock Changed");

			if (isMasterLocked)
			{
				masterLockIcons[0].SetActive(true);
				masterLockIcons[1].SetActive(false);

				if(!PSharpPlayer.LocalPlayer.IsMaster)
				{

					urlInput.interactable = false;
					timelineSlider.interactable = false;
					loopButton.interactable = false;
					pauseResumeButton.interactable = false;
					masterLockButton.interactable = false;

					urlInput.placeholder.GetComponent<TMP_Text>().text = $"Only {_masterName} can control the video player";
				}
				else
				{
					urlInput.interactable = true;
					timelineSlider.interactable = true;
					loopButton.interactable = true;
					pauseResumeButton.interactable = true;
					masterLockButton.interactable = true;

					urlInput.placeholder.GetComponent<TMP_Text>().text = "Enter Video URL...";
				}
			}
			else
			{
				masterLockIcons[0].SetActive(false);
				masterLockIcons[1].SetActive(true);

				urlInput.interactable = true;
				timelineSlider.interactable = true;
				loopButton.interactable = true;
				pauseResumeButton.interactable = true;

				if(PSharpPlayer.LocalPlayer.IsMaster)
				{
					masterLockButton.interactable = true;
				}
				else 
				{
					masterLockButton.interactable = false;
				}
				urlInput.placeholder.GetComponent<TMP_Text>().text = "Enter Video URL...";
			}
		}

		private void OnLoopChanged()
		{

			if (_isLooping)
			{
				if(loopButtonBackground) loopButtonBackground.color = buttonActivatedColor;
				if(loopButtonIcon) loopButtonIcon.color = iconInvertedColor;
				videoProvider.Loop = true;
			}
			else
			{
				if(loopButtonBackground) loopButtonBackground.color = buttonBackgroundColor;
				if(loopButtonIcon) loopButtonIcon.color = whiteGraphicColor;
                videoProvider.Loop = false;
            }
		}

		private void AddVideo(string url)
		{

			_currentUrl = url;
		}

		public void Awake()
		{
			
			urlInput.onSubmit.AddListener((url) => 
			{
				urlInput.SetTextWithoutNotify(string.Empty);
				if (!IsOwner)
				{
					SendNetworkedEvent(NetworkEventTarget.Owner, nameof(OnRequestAddVideo), url);
					return;
				}

				AddVideo(url);
			});
			pauseResumeButton.onClick.AddListener(() => 
			{
				if(!IsOwner && !isMasterLocked)
				{
					SendNetworkedEvent(NetworkEventTarget.Owner, nameof(OnRequestPlayPause));
					return;
				}

				_isPaused = !_isPaused;

			});

			masterLockButton.onClick.AddListener(() => 
			{
				if(PSharpPlayer.LocalPlayer.IsMaster)
				{
					PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
					isMasterLocked = !isMasterLocked;
				}
						
			});

			muteButton.onClick.AddListener(() => 
			{
				_isMuted = !_isMuted;
				
				if(_isMuted)
				{
					videoProvider.Mute(true);
						
					muteIcons[0].SetActive(false);
					muteIcons[1].SetActive(true);
				}
				else {
                    videoProvider.Mute(false);
                    muteIcons[0].SetActive(true);
					muteIcons[1].SetActive(false);
				}

			});

			reloadButton.onClick.AddListener(() =>
			{
				if (!IsOwner)
					PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
				ReloadVideo();
			});

			loopButton.onClick.AddListener(() =>
			{
				if (!IsOwner && !isMasterLocked)
				{
					SendNetworkedEvent(NetworkEventTarget.Owner, nameof(OnRequestLoopButton));
					return;
				}

				_isLooping = !_isLooping;

			});

			infoButton.onClick.AddListener(() => 
			{
				foreach (GameObject obj in infoObjects)
				{
					obj.SetActive(!obj.activeSelf);
				}
			});

			timelineSlider.onValueChanged.AddListener((v) =>
			{
				if(isMasterLocked)
				{
					if(PSharpPlayer.LocalPlayer.IsMaster)
					{
						if (!IsOwner)
							PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);

						videoProvider.Time = videoProvider.duration * v;
					}
					return;
				}
				else
				{
					if (!IsOwner)
						PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);

					videoProvider.Time = videoProvider.duration * v;
				}
				
			});

			if(playbackSlider != null)
			{
				playbackSlider.onValueChanged.AddListener((v) =>
				{
					if(!videoProvider.isLiveStream)
					{
						if (isMasterLocked)
						{
							if(PSharpPlayer.LocalPlayer.IsMaster || !IsOwner)
							{
								if(!IsOwner)
								{
									PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
								}

								playbackRate = playbackSlider.value;
							}	
						}
						else
						{
							if (!IsOwner)
							{
								PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
							}

							playbackRate = playbackSlider.value;
						}
					}
					else
					{
						playbackSlider.value = 1;
						playbackRate = 1;
					}
				
				});
			}
			

			videoProvider.OnVideoLoading += () =>
			{
				videoProvider.Stop();

				playbackSlider.interactable = false;
				pauseResumeButton.interactable = false;
				loopButton.interactable = false;
				reloadButton.interactable = false;

				statusText.text = "Loading Video...";
			};

			videoProvider.OnVideoError += () =>
			{
                playbackSlider.interactable = false;
                pauseResumeButton.interactable = true;
				loopButton.interactable = true;
				reloadButton.interactable = true;

				statusText.text = "Error Loading Video";
			};

			videoProvider.OnVideoReady += () =>
			{
                if (videoProvider.isLiveStream)
                {
                    playbackSlider.interactable = false;
                }
                else
                {
                    playbackSlider.interactable = true;
                }

                playbackSlider.value = 1;
                playbackRate = 1;
                pauseResumeButton.interactable = true;
				loopButton.interactable = true;
				reloadButton.interactable = true;
			};

			videoProvider.OnVideoStarted += () =>
			{

                statusText.text = string.Empty;
				
				videoProvider.Time = _time;
				_isPaused = false;

				if(newJoinPause)
				{
					_isPaused = true;
					videoProvider.IsPaused = true;
					newJoinPause = false;
					_time = videoProvider.Time;
				}
                if (!videoProvider.isLiveStream)
                {
                    timelineSlider.interactable = true;
                    pauseResumeButton.interactable = true;

                    _videoLength = GetTime(videoProvider.duration);
                    SetTimeText(true);
                }
                else
                {
                    timelineSlider.value = 0;
                    statusText.text = "";

                    timelineSlider.interactable = false;
                    pauseResumeButton.interactable = false;
                }

                if (_isPaused)
                {
                    videoProvider.IsPaused = true;
                }
			};
			
			videoProvider.OnVideoEnd += () =>
			{
				if(IsOwner)
				{
					if(!_isLooping && !videoProvider.isLoading && !videoProvider.isPlaying)
					{
						_currentUrl = string.Empty;
					}
				}
			};

			StartCoroutine(Synchronizer());
		}

		[PSharpEvent]
		private void OnRequestAddVideo(string url)
		{

			if (!IsOwner) return;

			if (isMasterLocked)
			{
				if (PSharpPlayer.LocalPlayer.IsMaster)
				{
					_videoOwnerName = PSharpPlayer.LocalPlayer.Username;
					_time = 0;
					if (!string.IsNullOrEmpty(_currentUrl)) _lastVideo = _currentUrl;
					_currentUrl = url;
				}
			}
			else
			{
				_videoOwnerName = PSharpPlayer.LocalPlayer.Username;
				_time = 0;
				if (!string.IsNullOrEmpty(_currentUrl)) _lastVideo = _currentUrl;
				_currentUrl = url;
			}
		}

		[PSharpEvent]
		private void OnRequestPlayPause()
		{

			if (!IsOwner || isMasterLocked)
			{
				return;
			}
			else
			{
				_isPaused = !_isPaused;
			}

		}

		[PSharpEvent]
		private void OnRequestLoopButton()
		{

			if (!IsOwner || isMasterLocked)
			{ 
				return;
			}
			else
			{
				_isLooping = !_isLooping;
			}
		}

		private void SetTimeText(bool setSlider)
		{
			if (videoProvider.isLiveStream)
				return;

			statusText.text = $"{GetTime(videoProvider.Time)} / {_videoLength}";

			if (setSlider)
			{
				timelineSlider.SetValueWithoutNotify((float)(videoProvider.Time / videoProvider.duration));
			}
		}

        private void ReloadVideo() 
		{
			if (!string.IsNullOrEmpty(_currentUrl))
			{
				reloadButton.GetComponent<Animator>().SetTrigger("Rotate");
				videoProvider.PlayURL(_currentUrl);
			}
			
		}

        private IEnumerator Synchronizer()
        {
            while (true)
            {
                if (videoProvider.isPlaying)
                {
                    SetTimeText(true);

                    if (IsOwner)
                    {
                        _time = videoProvider.Time;
                    }
                }

                yield return new WaitForSeconds(1.0f);
            }
        }

        public override void OnNetworkReady()
		{
			_masterName = PSharpPlayer.MasterPlayer.Username;
			masterName.text = _masterName;

			if(!string.IsNullOrEmpty(video) && IsOwner )	_currentUrl = video;

			if(_isPaused)
			{
				newJoinPause = true;
			}
		}

		public override void OnMasterChanged(PSharpPlayer newMaster)
		{
			_masterName = newMaster.Username;
			masterName.text = _masterName;
			OnMasterLockChanged();
		}

		public string GetTime(double seconds)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

			return timeSpan.Hours > 0 ? timeSpan.ToString(@"h\:mm\:ss") : timeSpan.ToString(@"m\:ss");
		}
	}
}