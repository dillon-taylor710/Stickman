﻿using Fusion;
using FusionHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawBridgeController : NetworkBehaviour//WithState<NetworkStateTimer>
{
    [Networked] public ref NetworkStateTimer State => ref MakeRef<NetworkStateTimer>();
    public AXIS axis;
    public float min_value;
    public float max_value;
    public float speed;
    public float delay_time;

    private Vector3 value;
    private Vector3 old_angle;
    private int direction = 1;

    public TickTimer delayTimer;
    private float gap_time = 0f;

    public override void Spawned()
    {
        if (axis == AXIS.X)
        {
            value = new Vector3(1, 0, 0);
            old_angle = new Vector3(0, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);
        }
        if (axis == AXIS.Y)
        {
            value = new Vector3(0, 1, 0);
            old_angle = new Vector3(transform.localRotation.eulerAngles.x, 0, transform.localRotation.eulerAngles.z);
        }
        if (axis == AXIS.Z)
        {
            value = new Vector3(0, 0, 1);
            old_angle = new Vector3(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, 0);
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

        transform.localRotation = Quaternion.Euler(old_angle + value * State.sim);

        if (State.sim > max_value)
        {
            if (axis == AXIS.X)
            {
                transform.localRotation = Quaternion.Euler(new Vector3(max_value, old_angle.y, old_angle.z));
            }
            if (axis == AXIS.Y)
            {
                transform.localRotation = Quaternion.Euler(new Vector3(old_angle.x, max_value, old_angle.z));
            }
            if (axis == AXIS.Z)
            {
                transform.localRotation = Quaternion.Euler(new Vector3(old_angle.x, old_angle.y, max_value));
            }
            //GetComponent<Rigidbody>().velocity = Vector3.zero;
            direction = -1;
        }
        else if (State.sim < min_value)
        {
            if (axis == AXIS.X)
            {
                transform.localRotation = Quaternion.Euler(new Vector3(min_value, old_angle.y, old_angle.z));
            }
            if (axis == AXIS.Y)
            {
                transform.localRotation = Quaternion.Euler(new Vector3(old_angle.x, min_value, old_angle.z));
            }
            if (axis == AXIS.Z)
            {
                transform.localRotation = Quaternion.Euler(new Vector3(old_angle.x, old_angle.y, min_value));
            }
            //GetComponent<Rigidbody>().velocity = Vector3.zero;
            direction = 1;
        }
    }
}
