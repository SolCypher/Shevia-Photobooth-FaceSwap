using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingIndicator : MonoBehaviour
{
    public GameObject overlay;
    public GameObject icon;
    public float rotSpd = 200f;

    private void Start() {
        overlay.SetActive(false);
    }

    private void Update() {
        if(FaceSwapAPI.IsLoading){
            overlay.SetActive(true);
            icon.SetActive(true);
            icon.transform.Rotate(Vector3.forward, rotSpd*Time.deltaTime);

        }else{
            overlay.SetActive(false);
        }
    }

}
