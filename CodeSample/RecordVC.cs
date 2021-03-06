/*
 *      THE PROJECT 
 *      This script was part of a previz project that combined several technologies to allow directors to set up and test shots before they were staged in real life. 
*       Similar to the previs seen here https://thethirdfloorinc.com/3011/avengers-infinity-war-previs/ except the director would be able to traverse the scene in VR 
*       while being able to control time, speed, and direction of the scene, while holding or placing a camera and recording the scene. The recording would be output 
*       to a file for later viewing. Real actors could also be placed in the scene while being tracked with a Perception Neuron suit. 
*       
*       THE SCRIPT
*       This script combines Unity, HTC Vive, and RockVR to control the virtual camera in a scene and handle the Start, Stop, and Processing of created videos. 
*/
using UnityEngine;
using Valve.VR;
using RockVR.Video;
using System;
using System.IO;
using System.Diagnostics;

namespace VirtualCamera
{
    public class RecordVC : MonoBehaviour
    {
        [SerializeField]
        private string videoCaptureFolder = "Recordings";
        [SerializeField]
        private SteamVR_Action_Boolean recordingButton;
        [SerializeField]
        private VideoCaptureCtrl videoCaptureCtrl;
        [SerializeField]
        private Camera extraCamera;
        [SerializeField]
        private CameraLensChanger.CameraLensBag lensBag;

        public Action onRecordingStart;
        public Action onRecordingEnd;
        public Action onProssessingStart;
        public Action onProssessingEnd;

        public bool IsRecording { get; private set; } = false;
        public bool IsProssessing { get; private set; } = false;

        public DirectoryInfo CaptureDirectory { get; private set; } = null;

        private void Awake()
        {
            if (extraCamera != null && lensBag != null)
                lensBag.onLensChange += (lens) => { lens.ApplyCameraLens(extraCamera); };
            ValidateFolderPath();
            if (recordingButton != null)
                recordingButton.onStateDown += RecordingButtonPressed;
            enabled = false;
        }

        //verifies folder path and creates folder if does not exist
        private void ValidateFolderPath()
        {
            var projectPath = Application.dataPath;
#if UNITY_EDITOR
            // Remove the 'Assets' path
            projectPath = projectPath.Substring(0, projectPath.Length - 7);
#endif
            CaptureDirectory = new DirectoryInfo(Path.Combine(projectPath, videoCaptureFolder));
            if (CaptureDirectory.Exists == false)
                CaptureDirectory.Create();
            if (videoCaptureCtrl.GetComponent<VideoCapture>() != null)
                videoCaptureCtrl.GetComponent<VideoCapture>().customPathFolder = CaptureDirectory.FullName;
        }

        private void RecordingButtonPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            if (IsProssessing)
                return;
            if (IsRecording) StopCapture();
            else StartCapture();
        }

        private void StartCapture()
        {
            if (videoCaptureCtrl.status != VideoCaptureCtrl.StatusType.NOT_START)
                return;
            IsRecording = true;
            videoCaptureCtrl.StartCapture();
            if (onRecordingStart != null)
                onRecordingStart.Invoke();
        }

        private void StopCapture()
        {
            if (videoCaptureCtrl.status != VideoCaptureCtrl.StatusType.STARTED)
                return;
            IsRecording = false;
            videoCaptureCtrl.StopCapture();
            if (onRecordingEnd != null)
                onRecordingEnd.Invoke();
            Prossessing();
        }

        private void Prossessing()
        {
            enabled = true;
            IsProssessing = true;
            if (onProssessingStart != null)
                onProssessingStart.Invoke();
        }

        private void Update()
        {
            if (IsProssessing)
            {
                if (videoCaptureCtrl.status == VideoCaptureCtrl.StatusType.FINISH)
                    IsProssessing = false;

                if (IsProssessing == false)
                {
                    enabled = false;
                    videoCaptureCtrl.ResetCapture();
                    if (OpenedDirectoryOnce)
                        OpenDirectory(CaptureDirectory);
                    if (onProssessingEnd != null)
                        onProssessingEnd.Invoke();
                }
            }
        }

        private static bool OpenedDirectoryOnce = true;
        private static void OpenDirectory(DirectoryInfo directory)
        {
#if !UNITY_EDITOR
            OpenedDirectoryOnce = false;
#endif
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = directory.FullName,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
           
        }
    }
}