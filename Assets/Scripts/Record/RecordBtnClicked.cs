﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class RecordBtnClicked : MonoBehaviour {
		public Text TextBox;
		public Text ResponseText;
		public Button RecordBtn;
		public RawImage RecordImg;
		public RawImage StopImg;

		struct ClipData
		{
				public int samples;
		}

		const int HEADER_SIZE = 44;

		private int minFreq;
		private int maxFreq;
		private string deviceName;
		private bool onRecord = false;

		private bool micConnected = false;

		//A handle to the attached AudioSource
		private AudioSource goAudioSource;

		public string apiKey = "AIzaSyCO0ZlLL7LjbtSCkzqGZNaDKzJgeQ1IkwI";
	// Use this for initialization
	void Start () {
		RecordImg.enabled = true;
		StopImg.enabled = false;
		//Check if there is at least one microphone connected
		if(Microphone.devices.Length <= 0)
		{
				//Throw a warning message at the console if there isn't
				Debug.LogWarning("Microphone not connected!");
		}
		else //At least one microphone is present
		{
				//Set 'micConnected' to true
				micConnected = true;
				deviceName = Microphone.devices[0];

				//Get the default microphone recording capabilities
				Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);

				//According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...
				if(minFreq == 0 && maxFreq == 0)
				{
						//...meaning 44100 Hz can be used as the recording sampling rate
						maxFreq = 44100;
				}

				//Get the attached AudioSource component
				goAudioSource = this.GetComponent<AudioSource>();
		}
	}

	public void BtnClicked() {
		if (onRecord) {
			StopRecord();
		} else {
			StartRecord();
		}
		onRecord = !onRecord;
	}

	void StartRecord()
	{
		RecordImg.enabled = false;
		StopImg.enabled = true;
		//Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource
		goAudioSource.clip = Microphone.Start( null, true, 7, maxFreq); //Currently set for a 7 second clip
	}

	void StopRecord()
	{
		RecordImg.enabled = true;
		StopImg.enabled = false;
		if(Microphone.IsRecording(null))
		{
			float filenameRand = UnityEngine.Random.Range (0.0f, 10.0f);

			string filename = "testing" + filenameRand;

			Microphone.End(null); //Stop the audio recording

			Debug.Log( "Recording Stopped");

			if (!filename.ToLower().EndsWith(".wav")) {
					filename += ".wav";
			}

			var filePath = Path.Combine("testing", filename);
			filePath = Path.Combine(Application.persistentDataPath, filePath);
			Debug.Log("Created filepath string: " + filePath);

			// Make sure directory exists if user is saving to sub dir.
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			SavWav.Save (filePath, goAudioSource.clip); //Save a temporary Wav File
			Debug.Log( "Saving @ " + filePath);
			string apiURL = "http://www.google.com/speech-api/v2/recognize?output=json&lang=en-us&key=" + apiKey;
			string Response;

			Debug.Log( "Uploading " + filePath);
			Response = HttpUploadFile (apiURL, filePath, "file", "audio/wav; rate=44100");
			Debug.Log ("Response String: " +Response);

			try {
				var jsonresponse = SimpleJSON.JSON.Parse(Response);

				if (jsonresponse != null) {		
						string resultString = jsonresponse ["result"] [0].ToString ();
						var jsonResults = SimpleJSON.JSON.Parse (resultString);

						string transcripts = jsonResults ["alternative"] [0] ["transcript"].ToString ();

						Debug.Log ("transcript string: " + transcripts );
						TextBox.text = transcripts;

				} else {
						TextBox.text = "No response...";
				}
			} catch(Exception ex) {
				Debug.Log(string.Format("Error uploading file error: {0}", ex));
				TextBox.text = "err: "+ ex;
			} finally {
				goAudioSource.Play(); //Playback the recorded audio

				//File.Delete(filePath); //Delete the Temporary Wav file
			}
			Translate(TextBox.text);
		}
	}

	void Translate(string Input) {
		//TODO: phan tich TextBox.text
		ResponseText.text = "I don't understand what you mean...";
	}

	void OnGUI() 
	{
			//If there is a microphone
			if(micConnected)
			{
					//If the audio from any microphone isn't being recorded
					if(Microphone.IsRecording(null))
					{
						TextBox.text = "Recording in progress...";
					}
			}
			else // No microphone
			{
					//Print a red "Microphone not connected!" message at the center of the screen
					GUI.contentColor = Color.red;
					GUI.Label(new Rect(Screen.width/2-100, Screen.height/2-25, 200, 50), "Microphone not connected!");
			}
	}

	public string HttpUploadFile(string url, string file, string paramName, string contentType) {
			Debug.Log(string.Format("Uploading {0} to {1}", file, url));
			HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
			wr.ContentType = "audio/l16; rate=44100";
			wr.Method = "POST";
			wr.KeepAlive = true;
			wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

			Stream rs = wr.GetRequestStream();
			FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
			byte[] buffer = new byte[4096];
			int bytesRead = 0;
			while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0) {
					rs.Write(buffer, 0, bytesRead);
			}
			fileStream.Close();
			rs.Close();

			WebResponse wresp = null;
			try {
					wresp = wr.GetResponse();
					Stream stream2 = wresp.GetResponseStream();
					StreamReader reader2 = new StreamReader(stream2);

					string responseString =  string.Format("{0}", reader2.ReadToEnd());
					Debug.Log("HTTP RESPONSE" +responseString);
					return responseString;

			} catch(Exception ex) {
					Debug.Log(string.Format("Error uploading file error: {0}", ex));
					if(wresp != null) {
							wresp.Close();
							wresp = null;
							return "Error";
					}
			} finally {
					wr = null;
		
			}

			return "empty";
	}

	// Update is called once per frame
	void Update () {
		
	}
}
