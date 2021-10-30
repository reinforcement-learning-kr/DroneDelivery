using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class birdController : MonoBehaviour
{
   
    public GameObject birds;
    public int num;
    GameObject[] targets;
    //float spawnDelay = -1.5f;
    //float spawnTimer = 0f;
    public List<move> moveList = new List<move>();
    public List<Transform> birdTransList = new List<Transform>();

    public float minY = 0.5f;
    public float maxY = 2f;
    // Start is called before the first frame update

    public void Init()
    {
        targets = new GameObject[num];
        for (int cnt = 0; cnt < num; cnt++)
        {
            Vector3 init_pos = new Vector3(Random.Range(-30.0f, 30.0f), Random.Range(minY, maxY), Random.Range(-30.0f, 30.0f));
            if (cnt < num)
            {
                targets[cnt] = (GameObject)Instantiate(birds, init_pos, Quaternion.identity);
                moveList.Add(targets[cnt].GetComponent<move>());
                moveList[cnt].Initialize();
                birdTransList.Add(targets[cnt].GetComponent<Transform>());
            }
        }
    }

    public void Re_position()
    {
        for (int cnt = 0; cnt < num; cnt++)
        {
            Vector3 init_pos = new Vector3(Random.Range(-30.0f, 30.0f), Random.Range(minY, maxY), Random.Range(-30.0f, 30.0f));
            moveList[cnt].Re_position();
            birdTransList[cnt].localPosition = init_pos;
        }
    }

    public void updateElapsed(float Elapsed)
    {
        for (int cnt = 0; cnt < num; cnt++)
        {
            moveList[cnt].updateElapsed(Elapsed);
        }
    }
}
