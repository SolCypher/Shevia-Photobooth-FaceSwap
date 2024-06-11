using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenURL : MonoBehaviour
{
    public void OpenSwappedFaceURL(){
        if(string.IsNullOrEmpty(FaceSwapAPI.GetResultURL())){
            Debug.LogError("URL is still empty, GetResult() may not be called yet or do the face swap first to get the result!");
            return;

        }

        Application.OpenURL(FaceSwapAPI.GetResultURL());

    }
}
