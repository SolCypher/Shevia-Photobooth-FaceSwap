using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

#region Store JSON Data Format
[Serializable]
public class ImgProcessResponse{
    // Case Sensitive from the JSON file
    public string request_id;
    public string status;
    public string description;
    public string err_code;
}

[Serializable]
public class ResultResponse{
    // Case Sensitive from the JSON file
    public string request_id;
    public string status;
    public string duration;
    public string total_duration;
    public string result_url;
    public string masks;
    public string answers;
    public string keypoints;
    public string embedding;
}

[Serializable]
public class InputData
{
    public string swap_image;
    public string target_image;
}

[Serializable]
public class Urls
{
    public string cancel;
    public string get;
}

[Serializable]
public class Prediction
{
    public string id;
    public string model;
    public string version;
    public InputData input;
    public string logs;
    public string output;
    public bool data_removed;
    public string error;
    public string status;
    public string created_at;
    public Urls urls;
}

[Serializable]
public class InputText
{
    public string text;
}

[Serializable]
public class Metrics
{
    public float predict_time;
}

[Serializable]
public class UrlsResult
{
    public string cancel;
    public string get;
}

[Serializable]
public class PredictionResult
{
    public string id;
    public string model;
    public string version;
    public InputText input;
    public string logs;
    public string output;
    public string error;
    public string status;
    public string created_at;
    public bool data_removed;
    public string started_at;
    public string completed_at;
    public Metrics metrics;
    public Urls urls;
}
#endregion

#region Container to store JSONs
[Serializable]
public class GetImg{
    public ImgProcessResponse image_process_response; // Case Sensitive from the JSON file
}

[Serializable]
public class GetResult{
    public ResultResponse image_process_response; // Case Sensitive from the JSON file
}
#endregion

public class FaceSwapAPI : MonoBehaviour
{
    // Current Used API: api.market
    public static bool IsLoading{get; set;}

    // The URL to access/see the result of the swap
    public static string returnResultURL;

    [Header("Gender")]
    public bool IsMale;
    public bool IsFemale = true;

    [Header("Face Swap API")]
    // API URL for POST, where the API will be given images for swapping
    [Tooltip("API URL, where the API will be given images for swapping")]
    [HideInInspector] public string urlPost_MarketAPI = "https://api.magicapi.dev/api/v1/capix/faceswap/faceswap/v1/image"; 
    [HideInInspector] public string urlPost_ReplicateAPI = "https://api.replicate.com/v1/predictions";

    // API URL for the result, where the API will do the swapping
    [Tooltip("API URL, where the API will do the swapping")]
    [HideInInspector] public string urlResult_MarketAPI = "https://api.magicapi.dev/api/v1/capix/faceswap/result/";
    [HideInInspector] public string urlResult_ReplicateAPI; // Api Replicate (omniedgeio) = Get from output after sending request POST
    
    [HideInInspector] public string apiKey_MarketAPI = "clwsyb2g10004i7099prrvy4b";
    [HideInInspector] public string apiKey_ReplicateAPI; // Api Replicate (omniedgeio) = Set it Environmental Variables in the OS (Can also check web to get the token too)
    
    [Header("Choose 1 API to use")]
    public bool IsAPI_Market = true;
    public bool IsAPI_Replicate;

    [Header("Image Sources")]
    public RawImage resultImg;
    [Tooltip("The Base Image")]
    public List<string> baseImgUrlList_Male;
    public List<string> baseImgUrlList_Female;
    // public TMP_InputField targetImgURL;
    public RawImage targetImgPreview;
    [Tooltip("The Image to Swap to the Base Image")]
    public TMP_InputField swapImgURL;
    public RawImage swapImgPreview;

    [Header("Debug")]
    public TMP_Text errorChooseTxt1;
    public TMP_Text errorChooseTxt2;
    public TMP_Text errorChooseDebugTxt;
    public TMP_Text errorSwapTxt;

    private string requestID;
    private string selectedBaseImg;
    private bool TargetValid = false;
    private bool SwapValid = false;

    public static string GetResultURL(){
        return returnResultURL;
    }

    private void Start() {
        if(IsAPI_Market && IsAPI_Replicate){    
            Debug.LogError("Please ONLY choose 1 API to use!");
        }
        
        // Load the Replicate API TOKEN
        apiKey_ReplicateAPI = Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN");

        errorChooseTxt1.gameObject.SetActive(false);
        errorChooseTxt2.gameObject.SetActive(false);
        errorChooseDebugTxt.gameObject.SetActive(false);
        errorSwapTxt.gameObject.SetActive(false);

        // Add a listener to the InputField to trigger validation on value change
        // targetImgURL.onEndEdit.AddListener(delegate { ValidateAndLoadImage(targetImgURL.text, targetImgPreview); });
        // swapImgURL.onEndEdit.AddListener(delegate { ValidateAndLoadImage(swapImgURL.text, swapImgPreview); });
        swapImgURL.onEndEdit.AddListener(delegate { StartCoroutine(DownloadImage(swapImgURL.text, swapImgPreview)); });
    
        // [Check Replicate API Token]
        // apiKey = Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN");
        // Debug.Log("API Token: " + apiKey);
        // if (string.IsNullOrEmpty(apiKey))
        // {
        //     Debug.LogError("API token is not set!");
        //     return;
        // }

        // TestReplicateAPI();

    }

#region Check Replicate API (Active or not)
    void TestReplicateAPI()
    {
        StartCoroutine(CallReplicateAPI());
    }
    IEnumerator CallReplicateAPI()
    {
        using (UnityWebRequest webReq = new UnityWebRequest("https://api.replicate.com/v1/account"))
        {
            webReq.method = UnityWebRequest.kHttpVerbGET;
            webReq.downloadHandler = new DownloadHandlerBuffer();
            webReq.SetRequestHeader("Authorization", "Bearer " + apiKey_ReplicateAPI);

            yield return webReq.SendWebRequest();

             // Error Checker
            if(webReq.responseCode == 200){
                string responseJSON = webReq.downloadHandler.text;

                Debug.Log("[SUCCESS] Response JSON: " + responseJSON);
                // Debug.Log("Req ID: " + response.image_process_response.request_id);

            }else{
                Debug.LogError("[Error] " + webReq.responseCode + ": " + webReq.downloadHandler.text);
            }
        }
    }
#endregion

    public void ToggleOnMarketAPI(){
        IsAPI_Market = true;
        IsAPI_Replicate = false;
    }
    public void ToggleOnReplicateAPI(){
        IsAPI_Market = false;
        IsAPI_Replicate = true;
    }

    public void ToggleMale(){
        IsMale = true;
        IsFemale = false;
    }public void ToggleFemale(){
        IsMale = false;
        IsFemale = true;
    }

#region Send Requests Functions
    public void SendRequestFaceSwap(){
        if(IsAPI_Market && IsAPI_Replicate){
            Debug.LogError("Select only 1 API!");
            return;
        }

        // Random the Base Img
        BaseImgRandomizer();

        if(IsAPI_Market){
            StartCoroutine(FaceSwapReq_MarketAPI());

        }else if(IsAPI_Replicate){
            StartCoroutine(FaceSwapReq_ReplicateAPI());
        }

    }

    public void GetSwappedImg(){
        if(IsAPI_Market && IsAPI_Replicate){
            Debug.LogError("Select only 1 API!");
            return;
        }

        // Debug.Log("Req ID: " + requestID);
        if(IsAPI_Market){
            StartCoroutine(GetResult_MarketAPI());

        }else if(IsAPI_Replicate){
            StartCoroutine(GetResult_ReplicateAPI());
        }

    }
#endregion
    // Validate the URL to https and load the image if valid
    private void ValidateAndLoadImage(string url, RawImage img)
    {
        if (IsValidImageUrl(url))
        {
            // Set the Image Validation to true
            if(img == targetImgPreview){
                TargetValid = true;
                errorChooseTxt1.gameObject.SetActive(false);
            }else if(img == swapImgPreview){
                SwapValid = true;
                errorChooseTxt2.gameObject.SetActive(false);
            }

            // Ensure the URL uses HTTPS
            string secureUrl = ConvertToHttps(url);

            StartCoroutine(DownloadImage(secureUrl,img));
        }
        else
        {
            // Set the Image Validation to false
            if(img == targetImgPreview){
                TargetValid = false;
                errorChooseTxt1.text = "Invalid Base Image URL";
                errorChooseTxt1.gameObject.SetActive(true);
                targetImgPreview.texture = null;

                Debug.LogError("Invalid Base Image URL");
            }else if(img == swapImgPreview){
                SwapValid = false;
                errorChooseTxt2.text = "Invalid Swap Image URL";
                errorChooseTxt2.gameObject.SetActive(true);
                swapImgPreview.texture = null;

                Debug.LogError("Invalid Swap Image URL");
            }
        }
    }

    // Check if the URL is an Image URL or not (ends with .jpg, .png, etc..)
    private bool IsValidImageUrl(string url)
    {
        Uri uriResult;
        bool isValidUri = Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                          (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        
        if (isValidUri)
        {
            // Check if the URL ends with a valid image extension
            string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            foreach (string extension in validExtensions)
            {
                if (url.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Convert the URL to HTTPS (Unity only able to download from HTTPS URLs)
    private string ConvertToHttps(string url)
    {
        // If the URL starts with "http://", replace it with "https://"
        if (url.StartsWith("http://"))
        {
            return url.Replace("http://", "https://");
        }
        return url;
    }

    private void BaseImgRandomizer(){
        if(IsMale){
            if(baseImgUrlList_Male.Count <= 0){
            Debug.LogError("Input Base Img Urls first!");
            return;
            }
            int randomNum = Random.Range(0, baseImgUrlList_Male.Count);
            selectedBaseImg = baseImgUrlList_Male[randomNum];

        }else if(IsFemale){
            if(baseImgUrlList_Female.Count <= 0){
            Debug.LogError("Input Base Img Urls first!");
            return;
            }
            int randomNum = Random.Range(0, baseImgUrlList_Female.Count);
            selectedBaseImg = baseImgUrlList_Female[randomNum];
        }
       
    }

#region IEnumerator Requests
    // Assigns the Images
    private IEnumerator FaceSwapReq_MarketAPI()
    {
        // API Market
        Debug.Log("Using Market API");

        errorSwapTxt.color = Color.red;
        errorSwapTxt.gameObject.SetActive(false);

        // if(SwapValid == false){
        //     Debug.LogError("Please input the correct image URLs first!");
        //     errorSwapTxt.text = "Please input the correct image URLs first!";
        //     errorSwapTxt.gameObject.SetActive(true);
        //     yield break;
        // }

        // Set Loading to true
        IsLoading = true;

        // Create Form data (Webnya accept Curl format) [Under Request body]
        string targetUrl = selectedBaseImg; // targetImgURL.text
        string swapUrl = swapImgURL.text;
        string formData = "target_url=" + UnityWebRequest.EscapeURL(targetUrl) + "&swap_url=" + UnityWebRequest.EscapeURL(swapUrl);

        Debug.Log("Target URL: " + targetUrl);

        // Convert Form data to Byte Array
        byte[] body = Encoding.UTF8.GetBytes(formData);

        // Create UnityWebRequest
        UnityWebRequest webReq = new UnityWebRequest(urlPost_MarketAPI, "POST");
        webReq.uploadHandler = new UploadHandlerRaw(body);
        webReq.downloadHandler = new DownloadHandlerBuffer();

        // Set Headers
        webReq.SetRequestHeader("accept", "application/json");
        webReq.SetRequestHeader("x-magicapi-key", apiKey_MarketAPI);
        webReq.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        // Send the request 
        yield return webReq.SendWebRequest();

        // Error Checker
        if(webReq.responseCode == 200){
            string responseJSON = webReq.downloadHandler.text;
            GetImg response = JsonUtility.FromJson<GetImg>(responseJSON);
            requestID = response.image_process_response.request_id;

            Debug.Log("[Success (" + webReq.responseCode + ")]: Face Swap has completed");
            errorSwapTxt.color = Color.green;
            errorSwapTxt.text = "[Success] Face Swap has completed!";
            errorSwapTxt.gameObject.SetActive(true);
            // Debug.Log("Response JSON: " + responseJSON);
            // Debug.Log("Req ID: " + response.image_process_response.request_id);

        }else{
            Debug.LogError("[Error(" + webReq.responseCode + ")]: " + webReq.downloadHandler.text);
            errorSwapTxt.text = "[Error(" + webReq.responseCode + ")]: " + webReq.downloadHandler.text;
            errorSwapTxt.gameObject.SetActive(true);
        }
            
        // Set Loading to false
        IsLoading = false;

    }
    private IEnumerator FaceSwapReq_ReplicateAPI()
    {
        // API Replicate
        Debug.Log("Using Replicate API");

        errorSwapTxt.color = Color.red;
        errorSwapTxt.gameObject.SetActive(false);

        // if(SwapValid == false){
        //     Debug.LogError("Please input the correct image URLs first!");
        //     errorSwapTxt.text = "Please input the correct image URLs first!";
        //     errorSwapTxt.gameObject.SetActive(true);
        //     yield break;
        // }

        // Set Loading to true
        IsLoading = true;

        // Create Form data (Webnya accept Curl format) [Under Request body]
        string targetUrl = selectedBaseImg; // targetImgURL.text
        string swapUrl = swapImgURL.text;
        // Debug.Log("Target URL: " + targetUrl);

        string formData = "{\"version\": \"c2d783366e8d32e6e82c40682fab6b4c23b9c6eff2692c0cf7585fc16c238cfe\", \"input\": { \"swap_image\": \"" + swapUrl +
         "\", \"target_image\": \"" + targetUrl + "\" }}";
        
        // Convert Form data to Byte Array
        byte[] body = Encoding.UTF8.GetBytes(formData);

        // Create UnityWebRequest
        UnityWebRequest webReq = new UnityWebRequest(urlPost_ReplicateAPI, "POST");
        webReq.uploadHandler = new UploadHandlerRaw(body);
        webReq.downloadHandler = new DownloadHandlerBuffer();

        // Set Headers
        webReq.SetRequestHeader("Authorization", "Bearer " + apiKey_ReplicateAPI);
        webReq.SetRequestHeader("Content-Type", "application/json");

        // Send the request 
        yield return webReq.SendWebRequest();

        // Error Checker
        if((int)webReq.responseCode >= 200 && (int)webReq.responseCode < 300){
            string responseJSON = webReq.downloadHandler.text;
            Prediction response = JsonUtility.FromJson<Prediction>(responseJSON);
            requestID = response.id;
            urlResult_ReplicateAPI = response.urls.get;

            Debug.Log("[Success (" + webReq.responseCode + ")]: Face Swap has completed");
            errorSwapTxt.color = Color.green;
            errorSwapTxt.text = "[Success] Face Swap has completed!";
            errorSwapTxt.gameObject.SetActive(true);
            // Debug.Log("Response JSON: " + responseJSON);
            // Debug.Log("Req ID: " + response.id);
            // Debug.Log("URL Result Test: " + urlResult_ReplicateAPI);

        }else{
            Debug.LogError("[Error(" + webReq.responseCode + ")]: " + webReq.downloadHandler.text);
            Debug.LogError(webReq.error);
        }

        // Set Loading to false
        IsLoading = false;

    }

    // Request to get the Face Swap Image Result
    private IEnumerator GetResult_MarketAPI(){
        // API Market
        Debug.Log("Using Market API");

        if(string.IsNullOrEmpty(requestID)){
            errorSwapTxt.gameObject.SetActive(true);
            errorSwapTxt.text = "request_id is still empty, Request Face Swap first!";

            Debug.LogError("request_id is still empty, Request Face Swap first!");
            yield break;
        }

        // Set Loading to true
        IsLoading = true;
        
        bool IsCompleted = false;
        while(!IsCompleted){
            // Create Form Data (Curl) [Under Request body]
            string formdata = "request_id=" + requestID;

            // Convert Form Data > Byte Data
            byte[] body = Encoding.UTF8.GetBytes(formdata);

            // Create UnityWebRequest
            UnityWebRequest webReq = new UnityWebRequest(urlResult_MarketAPI, "POST");
            webReq.uploadHandler = new UploadHandlerRaw(body);
            webReq.downloadHandler = new DownloadHandlerBuffer();

            // Set Headers
            webReq.SetRequestHeader("accept", "application/json");
            webReq.SetRequestHeader("x-magicapi-key", apiKey_MarketAPI);
            webReq.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            // Send the request 
            yield return webReq.SendWebRequest();

            // Error Checker
            if(webReq.responseCode == 200){
                string responseJSON = webReq.downloadHandler.text;
                GetResult response = JsonUtility.FromJson<GetResult>(responseJSON);

                // 2nd Error checker, where if the image can't be processed by the server
                string status = response.image_process_response.status;
                if(status == "Error"){
                    errorSwapTxt.gameObject.SetActive(true);
                    errorSwapTxt.text = "Server failed to process request, try again or try changing the image";
                    IsLoading = false;

                    Debug.LogError("Server failed to process request, try again or try changing the image");
                    IsCompleted = true;

                }if(status == "OK"){
                    returnResultURL = response.image_process_response.result_url;

                    // Convert the URL to HTTPS
                    returnResultURL = ConvertToHttps(returnResultURL);

                    // Show the Result Img to the Raw Img UI
                    StartCoroutine(DownloadImage(returnResultURL, resultImg));

                    // Debug.Log("[Success] Result Link: " + returnResultURL);
                    errorSwapTxt.gameObject.SetActive(false);
                    // Debug.Log("Response JSON: " + responseJSON);
                    // Debug.Log("Req ID: " + response.image_process_response.result_url);

                    IsCompleted = true;
                    // Loading already Set to false from the DownloadImage();

                }else{
                    Debug.Log("Status: " + status + ". Checking again in 1 second...");
                    yield return new WaitForSeconds(1); // Wait for 1 second before checking again
                }

            }else{
                errorSwapTxt.gameObject.SetActive(true);
                errorSwapTxt.text = "[Error(" + webReq.responseCode + ")]: " + webReq.downloadHandler.text;
                Debug.LogError("[Error(" + webReq.responseCode + ")]: " + webReq.downloadHandler.text);

                IsCompleted = true;
                IsLoading = false;
            }
        }

    }
    private IEnumerator GetResult_ReplicateAPI(){
        // API Replicate
        Debug.Log("Using Replicate API");

        if(string.IsNullOrEmpty(requestID)){
            errorSwapTxt.gameObject.SetActive(true);
            errorSwapTxt.text = "request_id is still empty, Request Face Swap first!";

            Debug.LogError("request_id is still empty, Request Face Swap first!");
            yield break;
        }

        // Set Loading to true
        IsLoading = true;
        bool IsCompleted = false;
        while(!IsCompleted){
            string url = urlResult_ReplicateAPI;

            // Create UnityWebRequest
            UnityWebRequest webReq = UnityWebRequest.Get(url);
            webReq.downloadHandler = new DownloadHandlerBuffer();

            // Set Headers
            webReq.SetRequestHeader("Authorization", "Bearer " + apiKey_ReplicateAPI);

            // Send the request 
            yield return webReq.SendWebRequest();
            
            // Error Checker
            if(webReq.responseCode == 200){
                string responseJSON = webReq.downloadHandler.text;
                PredictionResult response = JsonUtility.FromJson<PredictionResult>(responseJSON);

                if(response.status == "succeeded"){
                    returnResultURL = response.output;
                    // Show the Result Img to the Raw Img UI
                    StartCoroutine(DownloadImage(returnResultURL, resultImg));

                    // Debug.Log("URL: " + url);
                    Debug.Log("[Success(" + webReq.responseCode + ")] Result Link: " + returnResultURL);
                    errorSwapTxt.gameObject.SetActive(false);
                    // Debug.Log("Response JSON: " + responseJSON);
                    // Debug.Log("Req ID: " + response.output);

                    IsCompleted = true;
                    // Loading already Set to false from the DownloadImage();

                }else if(response.status == "failed"){
                    errorSwapTxt.gameObject.SetActive(true);
                    errorSwapTxt.text = "Face swap request failed.";
                    Debug.LogError("Face swap request failed.");

                    IsCompleted = true;
                    IsLoading = false;  

                }else{
                    Debug.Log("Status: " + response.status + ". Checking again in 1 second...");
                    yield return new WaitForSeconds(1); // Wait for 1 second before checking again
            
                }

            }else{
                errorSwapTxt.gameObject.SetActive(true);
                errorSwapTxt.text = "[Error (" + webReq.responseCode + ")]: " + webReq.downloadHandler.text;
                Debug.LogError("[Error (" + webReq.responseCode + ")]: " + webReq.downloadHandler.text);

                IsCompleted = true;
                IsLoading = false;
            }
        }

    }
#endregion

    // Download & Set the Img URL to the corresponding RawImage UI for Preview
    private IEnumerator DownloadImage(string MediaUrl, RawImage img){   
        // Convert to HTTPS
        ConvertToHttps(MediaUrl);

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);

        yield return request.SendWebRequest();

        if(request.result != UnityWebRequest.Result.Success){
            if(img == resultImg){
                errorSwapTxt.gameObject.SetActive(true);
                errorSwapTxt.text = "[Error] " + request.error;

            }else if(img == targetImgPreview || img == swapImgPreview){
                errorChooseDebugTxt.gameObject.SetActive(true);
                errorChooseDebugTxt.text = "[Error] " + request.error;
            }

            Debug.LogError(request.error);
        }else{
            // Debug.Log("Showing Result Img...");
            img.texture = ((DownloadHandlerTexture) request.downloadHandler).texture;
            if(img == resultImg){
                errorSwapTxt.gameObject.SetActive(false);
            }else if(img == targetImgPreview || img == swapImgPreview){
                errorChooseDebugTxt.gameObject.SetActive(false);
            }
        }

        if(img == resultImg && IsLoading == true){
            // Set Loading to false
            IsLoading = false;
        }
    } 

}
