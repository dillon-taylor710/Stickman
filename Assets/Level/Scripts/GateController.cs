using Fusion;
using FusionHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GateController : NetworkBehaviour//WithState<NetworkStateTimer>
{
    [Networked] public ref NetworkStateTimer State => ref MakeRef<NetworkStateTimer>();
    public AXIS axis;
    public float min_value;
    public float max_value;
    public float speed;
    public float delay_time;

    private Vector3 value;
    private int direction = 1;
    private Vector3 old_pos;

    public TickTimer delayTimer;
    private float gap_time = 0f;

    public override void Spawned()
    {
        if (axis == AXIS.X)
        {
            value = transform.right;
            old_pos = new Vector3(0, transform.localPosition.y, transform.localPosition.z);
        }
        if (axis == AXIS.Y)
        {
            value = transform.up;
            old_pos = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
        }
        if (axis == AXIS.Z)
        {
            value = transform.forward;
            old_pos = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
        }

        if (Object.HasStateAuthority)
        {
            delayTimer = TickTimer.CreateFromSeconds(Runner, delay_time);
            State.sim = min_value;
        }
    }
    // Update is called once per frame
    public override void Render()
    {
        if (Object.HasStateAuthority && !delayTimer.Expired(Runner)) return;

        if (Object.HasStateAuthority)
        {
            gap_time += Time.deltaTime;
            if (gap_time > Constants.obstacle_change_gap)
            {
                State.sim += direction * speed * gap_time;
                gap_time = 0;
            }
        }

        transform.localPosition = old_pos + value * State.sim;
        if (axis == AXIS.X)
        {
            if (transform.localPosition.x > max_value)
            {
                transform.localPosition = new Vector3(max_value, old_pos.y, old_pos.z);
                //GetComponent<Rigidbody>().velocity = Vector3.zero;
                direction = -1;
            }
            else if (transform.localPosition.x < min_value)
            {
                transform.localPosition = new Vector3(min_value, old_pos.y, old_pos.z);
                //GetComponent<Rigidbody>().velocity = Vector3.zero;
                direction = 1;
            }
        }
        else if (axis == AXIS.Y)
        {
            if (transform.localPosition.y > max_value)
            {
                transform.localPosition = new Vector3(old_pos.x, max_value, old_pos.z);
                //GetComponent<Rigidbody>().velocity = Vector3.zero;
                direction = -1;
            }
            else if (transform.localPosition.y < min_value)
            {
                transform.localPosition = new Vector3(old_pos.x, min_value, old_pos.z);
                //GetComponent<Rigidbody>().velocity = Vector3.zero;
                direction = 1;
            }
        }
        else if (axis == AXIS.Z)
        {
            if (transform.localPosition.z > max_value)
            {
                transform.localPosition = new Vector3(old_pos.x, old_pos.y, max_value);
                //GetComponent<Rigidbody>().velocity = Vector3.zero;
                direction = -1;
            }
            else if (transform.localPosition.z < min_value)
            {
                transform.localPosition = new Vector3(old_pos.x, old_pos.y, min_value);
                //GetComponent<Rigidbody>().velocity = Vector3.zero;
                direction = 1;
            }
        }
    }
}
