using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour
{
    GameObject[] targets;
    Vector3 target_pos;
    public GameObject Waypoint;
    public bool switch_object = false;
    public float speed = 1.0f;
    int cnt = 0;
    public float time = 1.0f;
    public int way_num = 8;
    Rigidbody rb;

    [SerializeField] [Range(0f, 10f)] private float speed2 = 1.0f;
    // [SerializeField] [Range(0f, 10f)] private float length = 1f;

    private float runningTime = 0f;
    private float yPos = 0f;
    private float sine_y = 0f;
    private float init_y;
    // Start is called before the first frame update

    public void Initialize()
    {
        targets = new GameObject[way_num];
        for (int i = 0; i < way_num; i++)
        {
            Vector3 init_pos = new Vector3(Random.Range(-30.0f, 30.0f), Random.Range(5.0f, 12.0f), Random.Range(-30.0f, 30.0f));
            targets[i] = (GameObject)Instantiate(Waypoint, init_pos, Quaternion.identity);
        }
        //targets = GameObject.FindGameObjectsWithTag("bird_waypoint");

        this.rb = GetComponent<Rigidbody>();
        target_pos = targets[0].GetComponent<Transform>().localPosition;

        // lookahead a first target.
        yPos = this.transform.position.y;
        Vector3 relativePos = target_pos - this.transform.position;
        if (relativePos != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(relativePos);
        speed = Random.Range(0.7f, 3.0f);
    }

    public void Re_position()
    {
        for (int i = 0; i < way_num; i++)
        {
            Vector3 init_pos = new Vector3(Random.Range(-30.0f, 30.0f), Random.Range(5.0f, 12.0f), Random.Range(-30.0f, 30.0f));
            targets[i].GetComponent<Transform>().localPosition = init_pos; 
        }
    }

    public void updateElapsed(float Elapsed)
    {
        if (null == targets)
            return;

        if (switch_object)
        {
           // Debug.Log(switch_object);
            foreach (GameObject target in targets)
            {
                target.GetComponent<Renderer>().enabled = false;
                //target.SetActive(switch_object);
            }
        }
        else
        {
            foreach (GameObject target in targets)
            {
                target.GetComponent<Renderer>().enabled = true;
                //target.SetActive(switch_object);
            }
        }

        float step = speed * Time.deltaTime;
        //Debug.Log(this.transform.position.x - target_pos.x);
        //Debug.Log(this.transform.position.z - target_pos.z);
        if (Mathf.Abs(this.transform.position.x - target_pos.x) <= 2.0f && Mathf.Abs(this.transform.position.z - target_pos.z) <= 2.0f)
        {
            cnt++;
            if (targets.Length == cnt)
            {
                cnt = 0;
            }
            target_pos = targets[cnt].GetComponent<Transform>().localPosition;
        }
        //this.transform.position = new Vector3(this.transform.position.x, yPos, this.transform.position.z);
        //targets[cnt].transform.position = new Vector3(target_pos.x, yPos, target_pos.z);
        runningTime += Time.deltaTime * speed2;
        sine_y = Mathf.Sin(runningTime) * 0.25f;

        Vector3 relativePos = target_pos - this.transform.position;
        if (relativePos != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(relativePos);
        this.transform.position = Vector3.MoveTowards(new Vector3(this.transform.position.x, yPos + sine_y, this.transform.position.z), target_pos, step);
        //this.transform.position = Vector3.MoveTowards(this.transform.position, target_pos, step);
        //this.transform.position = Vector3.MoveTowards(this.transform.position, Vector3.Lerp(this.transform.position, target_pos, time), step);
    }

    private void OnCollisionStay(Collision collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Obstacle":
            case "building":
                ChangeTarget();
                break;
        }
    }

    public void ChangeTarget()
    {
        if (null == targets)
            return;

        int target_count = targets.Length;
        int rand = Random.Range(0, target_count);

        target_pos = targets[rand].GetComponent<Transform>().localPosition;
    }
}
