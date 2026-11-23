using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class PlayFabConnection : MonoBehaviour
{
    public string titleId = "";
    public string Status { get; private set; } = "Enter your title ID and test player credentials.";
    public bool Busy { get; private set; }
    [Serializable] class Credentials
    {
        public string TitleId, Username, Password;
        public bool RequireBothUsernameAndEmail = false;
    }
    [Serializable] class Envelope { public Result data; public string errorMessage; }
    [Serializable] class Result { public string PlayFabId; }

    public void Authenticate(string username, string password, bool register)
    {
        if (Busy) return;
        if (!Regex.IsMatch(titleId.Trim(), "^[a-zA-Z0-9]+$") || string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        { Status = "A title ID, test player username, and password are required."; return; }
        StartCoroutine(Send(username, password, register));
    }
    IEnumerator Send(string username, string password, bool register)
    {
        Busy = true;
        Status = "Connecting to PlayFab...";
        var body = new Credentials { TitleId = titleId.Trim(), Username = username, Password = password };
        string method = register ? "RegisterPlayFabUser" : "LoginWithPlayFab";
        using (var request = new UnityWebRequest("https://" + body.TitleId + ".playfabapi.com/Client/" + method, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 20;
            yield return request.SendWebRequest();
            Envelope result = null;
            try { result = JsonUtility.FromJson<Envelope>(request.downloadHandler.text); }
            catch (Exception) { }
            Status = request.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(result?.data?.PlayFabId)
                ? "Success — player " + result.data.PlayFabId
                : "PlayFab failed: " + (result?.errorMessage ?? "check connection and title ID.");
        }
        Busy = false;
    }
}
