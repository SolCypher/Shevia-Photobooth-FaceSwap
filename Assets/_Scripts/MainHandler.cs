using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainHandler : MonoBehaviour
{
    public CanvasGroup chooseImgsBtn;
    public CanvasGroup faceSwapResultBtn;

    private void Start() {
        faceSwapResultBtn.alpha = 0;
        faceSwapResultBtn.gameObject.SetActive(false);
        chooseImgsBtn.alpha = 1;
    }

    public void OpenChooseImg(){
        faceSwapResultBtn.alpha = 0;
        faceSwapResultBtn.gameObject.SetActive(false);

        chooseImgsBtn.alpha = 0;
        chooseImgsBtn.gameObject.SetActive(true);
        LeanTween.alphaCanvas(chooseImgsBtn, 1, 0.5f);
    }

    public void OpenFaceSwapResult(){
        chooseImgsBtn.alpha = 0;
        chooseImgsBtn.gameObject.SetActive(false);

        faceSwapResultBtn.alpha = 0;
        faceSwapResultBtn.gameObject.SetActive(true);
        LeanTween.alphaCanvas(faceSwapResultBtn, 1, 0.5f);
    }
}
