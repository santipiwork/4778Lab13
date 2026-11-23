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
    [Serializable] class Envelope
    {
        public Result data;
        public string error, errorMessage;
        public ErrorDetails errorDetails;
    }
    [Serializable] class ErrorDetails
    {
        public string[] Username, Password, Email, TitleId;
    }
    [Serializable] class Result { public string PlayFabId; }

    public bool Authenticate(string username, string password, bool register)
    {
        if (Busy) return false;
        titleId = (titleId ?? "").Trim();
        username = (username ?? "").Trim();
        if (!Regex.IsMatch(titleId, "^[a-zA-Z0-9]+$") || string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        { Status = "A title ID, test player username, and password are required."; return false; }
        if (register && (username.Length < 3 || username.Length > 20))
        { Status = "Choose a test-player username with 3–20 characters."; return false; }
        if (register && (password.Length < 6 || password.Length > 100))
        { Status = "Choose a test-player password with 6–100 characters."; return false; }
        StartCoroutine(Send(username, password, register));
        return true;
    }
    IEnumerator Send(string username, string password, bool register)
    {
        Busy = true;
        Status = "Connecting to PlayFab...";
        var body = new Credentials { TitleId = titleId.Trim(), Username = username, Password = password };
        string method = register ? "RegisterPlayFabUser" : "LoginWithPlayFab";
        try
        {
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
                ? (register ? "Registered and signed in" : "Signed in") + " — player " + result.data.PlayFabId
                    + ". Find this ID under Identity > Players in Game Manager."
                : DescribeFailure(result, request.responseCode, request.error);
        }
        }
        finally { Busy = false; }
    }

    static string DescribeFailure(Envelope result, long httpCode, string networkError)
    {
        if (httpCode == 0) return "Could not reach PlayFab: " + networkError;
        var message = new StringBuilder("PlayFab: " + (result?.errorMessage ?? "Unexpected response") + " (HTTP " + httpCode + ").");
        void Add(string field, string[] errors)
        {
            if (errors != null && errors.Length > 0) message.Append("\n" + field + ": " + string.Join(" ", errors));
        }
        var details = result?.errorDetails;
        if (details != null)
        {
            Add("Username", details.Username);
            Add("Password", details.Password);
            Add("Email", details.Email);
            Add("Title ID", details.TitleId);
        }
        if (result?.error == "InvalidParams")
            message.Append("\nFor registration, use a new 3–20 character username and a 6–100 character password. Use a player username, not your developer email.");
        else if (result?.error == "UsernameNotAvailable")
            message.Append("\nThat username is already registered. Log in with its password or choose a different username.");
        else if (result?.error == "AccountNotFound")
            message.Append("\nRegister a test player first. Creating the Weather Lab title does not create a player.");
        return message.ToString();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (Busy) Status = "Request interrupted. Try again.";
        Busy = false;
    }
}
