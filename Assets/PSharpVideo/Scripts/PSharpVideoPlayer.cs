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

		[Space]
		[Space]

		public PVR_VideoProvider videoProvider;

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

		[Space]
		
		public GameObject[] masterLockIcons;
		public GameObject[] muteIcons;
		public GameObject[] pauseResumeIcons;
		public GameObject[] infoObjects;

		[Space]

		public TMP_Text placeholderText;
		public TMP_Text statusText;
		public TMP_Text masterName;
		public TMP_Text videoOwner;

		[Space]

		public AudioSource speaker;


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

		[Space]
		[Header("Style Colors")]
		
		public Color buttonActivatedColor = new Color(1f, 1f, 1f, 1f);
    public Color iconInvertedColor = new Color(1f, 1f, 1f, 1f);
		public Color whiteGraphicColor = new Color(0.9433f, 0.9433f, 0.9433f);
    public Color buttonBackgroundColor = new Color(1f, 1f, 1f, 1f);

		private void OnURLChanged() 
		{

			if (!string.IsNullOrEmpty(_currentUrl) && !_isPaused)
			{
				currentVideo.text = _currentUrl;
				videoProvider.Play(_currentUrl);
			}
		}

		private void OnTimeChanged()
		{
			if (videoProvider.isLoadingVideo || IsObjectOwner)
				return;
	
			float desync = (float)_time - (float)videoProvider.videoPlayer.time;
	
			if (Mathf.Abs(desync) > 4)
			{
				videoProvider.videoPlayer.time = _time;
				if (!videoProvider.videoPlayer.isPlaying && !_isPaused) videoProvider.videoPlayer.Play();
			}
		}

		private void OnVideoOwnerChanged()
		{
			videoOwner.text = _videoOwnerName;
		}

		private void OnLastVideoChanged()
		{
			lastVideo.text = _lastVideo;
		}

		private void OnPlayPauseChanged()
		{
			if(_isPaused)
			{
				pauseResumeIcons[0].SetActive(true);
				pauseResumeIcons[1].SetActive(false);
				videoProvider.videoPlayer.Pause();
			}
			else 
			{
				pauseResumeIcons[0].SetActive(false);
				pauseResumeIcons[1].SetActive(true);
				videoProvider.videoPlayer.Play();
			}
		}

		private void OnMasterLockChanged()
		{
			if(isMasterLocked)
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

					placeholderText.text = $"Only {_masterName} can control the video player";
				}
				else
				{
					urlInput.interactable = true;
					timelineSlider.interactable = true;
					loopButton.interactable = true;
					pauseResumeButton.interactable = true;
					masterLockButton.interactable = true;

					placeholderText.text = "Enter Video URL...";
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
				placeholderText.text = "Enter Video URL...";
			}
		}

		private void OnLoopChanged()
		{
			if(_isLooping)
			{
				if(loopButtonBackground) loopButtonBackground.color = buttonActivatedColor;
				if(loopButtonIcon) loopButtonIcon.color = iconInvertedColor;
			}
			else
			{
				if(loopButtonBackground) loopButtonBackground.color = buttonBackgroundColor;
				if(loopButtonIcon) loopButtonIcon.color = whiteGraphicColor;
			}
		}

		public void Awake()
		{
			urlInput.onSubmit.AddListener((url) => 
			{
				if(!IsObjectOwner)
					PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
				if(isMasterLocked)
				{
					if(PSharpPlayer.LocalPlayer.IsMaster)
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
				
				urlInput.SetTextWithoutNotify(string.Empty);
			});
			pauseResumeButton.onClick.AddListener(() => 
			{
				if(isMasterLocked)
				{
					if(PSharpPlayer.LocalPlayer.IsMaster)
					{
						if (!IsObjectOwner)
							PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
						_isPaused = !_isPaused;
					}
					return;
				}
				else
				{
					if (!IsObjectOwner)
						PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
					_isPaused = !_isPaused;
				}
				
			});

			masterLockButton.onClick.AddListener(() => 
			{

				if (!IsObjectOwner)
				{
					PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);	
				}
				if(PSharpPlayer.LocalPlayer.IsMaster)
						isMasterLocked = !isMasterLocked;
			});

			muteButton.onClick.AddListener(() => 
			{
				_isMuted = !_isMuted;
				
				if(_isMuted)
				{
					Debug.Log("Muted? Yes");
					speaker.mute = true;
					muteIcons[0].SetActive(false);
					muteIcons[1].SetActive(true);
				}
				else {
					Debug.Log("Muted? No");
					speaker.mute = false;
					muteIcons[0].SetActive(true);
					muteIcons[1].SetActive(false);
				}

			});

			reloadButton.onClick.AddListener(() =>
			{
				if (!IsObjectOwner)
					PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
				ReloadVideo();
			});

			loopButton.onClick.AddListener(() =>
			{
				if(isMasterLocked)
				{
					if(PSharpPlayer.LocalPlayer.IsMaster)
					{
						if(!IsObjectOwner)
							PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
						_isLooping = !_isLooping;
					}
					return;
				}
				else
				{
					if(!IsObjectOwner)
						PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);
					_isLooping = !_isLooping;
				}
				
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
						if (!IsObjectOwner)
							PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);

						videoProvider.videoPlayer.time = videoProvider.videoPlayer.length * v;
					}
					return;
				}
				else
				{
					if (!IsObjectOwner)
						PSharpNetworking.SetOwner(PSharpPlayer.LocalPlayer, gameObject);

					videoProvider.videoPlayer.time = videoProvider.videoPlayer.length * v;
				}
				
			});

			videoProvider.OnVideoLoading += () =>
			{
				videoProvider.videoPlayer.Stop();


				pauseResumeButton.interactable = false;
				loopButton.interactable = false;
				reloadButton.interactable = false;

				statusText.text = "Loading Video...";
			};

			videoProvider.OnVideoError += () =>
			{
				pauseResumeButton.interactable = true;
				loopButton.interactable = true;
				reloadButton.interactable = true;

				statusText.text = "Error Loading Video";
			};

			videoProvider.OnVideoReady += () =>
			{
				pauseResumeButton.interactable = true;
				loopButton.interactable = true;
				reloadButton.interactable = true;
			};

			videoProvider.OnVideoStarted += () =>
			{
				statusText.text = string.Empty;
				
				videoProvider.videoPlayer.time = _time;

				_videoLength = GetTime(videoProvider.videoPlayer.frameCount / videoProvider.videoPlayer.frameRate);
				statusText.text = $"{GetTime(videoProvider.videoPlayer.time)} / {_videoLength}";

			};
			
			videoProvider.OnVideoEnd += () =>
			{
				if(IsObjectOwner)
				{
					if(!_isLooping)
					{
						_currentUrl = string.Empty;
					}
					else 
					{
						_time = 0;
						videoProvider.videoPlayer.Play();
					}
				}
			};

			StartCoroutine(Synchronizer());
		}

		private void ReloadVideo() 
		{
			if (!string.IsNullOrEmpty(_currentUrl) && !_isPaused)
			{
				reloadButton.GetComponent<Animator>().SetTrigger("Rotate");
				videoProvider.Play(_currentUrl);
			}
			
		}

		public IEnumerator Synchronizer()
		{
			while (true)
			{
				if (videoProvider.videoPlayer.isPlaying)
				{
					statusText.text = $"{GetTime(videoProvider.videoPlayer.time)} / {_videoLength}";

					timelineSlider.SetValueWithoutNotify((float)(videoProvider.videoPlayer.time / videoProvider.videoPlayer.length));

					if (IsObjectOwner)
					{
						_time = videoProvider.videoPlayer.time;
					}
				}

				yield return new WaitForSeconds(1.0f);
			}
		}

    public override void OnNetworkReady()
    {
			_masterName = PSharpPlayer.MasterPlayer.Username;
			masterName.text = _masterName;
			if(!string.IsNullOrEmpty(video) && IsObjectOwner )	_currentUrl = video;
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