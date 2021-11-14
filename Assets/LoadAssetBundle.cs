using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class LoadAssetBundle : MonoBehaviour
{
    public NNModel onnxModel2 = null;
    public ModelManager modelManager = null;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(InstantiateObject());    //ȣ�� ����
    }

    public void Callback(NNModel onnxModel_)
    {
        onnxModel2 = onnxModel_;
        ModelManager.onnxModel = onnxModel_;
        SceneManager.LoadScene("InferenceScene_ML_Agent");
    }

    IEnumerator InstantiateObject()
    {
        //�ҷ��� ���. ���� ���ϸ� Ȥ�� �ٿ�ε� url.
        string uri = "file:///" + Application.dataPath + "/AssetBundles/" + "model";

        using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, 0))
        {

            yield return request.SendWebRequest();

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);    //������ ���� ����

            NNModel onnxMode = bundle.LoadAsset<NNModel>("MyBehavior");

            Debug.Log(onnxMode);
            if (request.isDone)
            {
                Callback(onnxMode);
            }
        }
    }
}
