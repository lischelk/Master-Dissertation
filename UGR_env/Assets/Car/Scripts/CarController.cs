using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Car")]
    public Rigidbody car;
    public List<AxleInfo> axleInfos;

    [Header("Car Specs")]
    public float motorForce;
    public float breakForce;
    public float decelerationForce;
    public float maxSteerAngle;
    public float maxSpeed;
    public float turnspeed = 5;
    public float wheelP, wheelI, wheelD;

    private bool respawned = false;
    private bool resetGrounded = true;

    public delegate void ExitAction();
    public static event ExitAction NotGrounded;
    public static event ExitAction AllNotGrounded;

    private PID wheelPIDControler;

    private void Start()
    {
        car.centerOfMass += new Vector3(0f, -0.3f, -0.4f);
        wheelPIDControler = new PID(wheelP, wheelI, wheelD);
    }

    private void FixedUpdate()
    {
        if (respawned)
        {
            wheelPIDControler.ResetPID();
            ApplyBreaking(float.MaxValue);
            car.isKinematic = true;
            respawned = false;
        }
        else
        {
            car.isKinematic = false;
        }
        /*foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.motor)
            {
                WheelHit hit;
                if (axleInfo.leftWheel.GetGroundHit(out hit))
                {
                    if (hit.sidewaysSlip / axleInfo.leftWheel.sidewaysFriction.extremumSlip > 1)
                        Debug.Log("Slip");
                }
            }
        }*/
        //wheelPIDControler.Kp = wheelP;
        //wheelPIDControler.Ki = wheelI;
        //wheelPIDControler.Kd = wheelD;
    }

    private void Update()
    {
        if (AWheelNotGrounded())
        {
            if (resetGrounded) return;
            NotGrounded();
            if (AllWheelsNotGrounded())
            {
                AllNotGrounded();
            }
        }
        resetGrounded = false;
    }

    // throttle [0,1], steering [-1,1], brake [0,1]
    public void SetInputs(float throttle, float steering, float brake)
    {
        car.isKinematic = false;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            // Set motor torque
            if (axleInfo.motor)
            {
                axleInfo.leftWheel.motorTorque = throttle * motorForce;
                axleInfo.rightWheel.motorTorque = throttle * motorForce;
            }

            // Set steering angle
            if (axleInfo.steering)
            {
                float error = steering * maxSteerAngle - axleInfo.leftWheel.steerAngle;
                float wheelCorrection = wheelPIDControler.GetOutput(error, Time.deltaTime);

                float newWheelAngle = Mathf.Lerp(axleInfo.leftWheel.steerAngle, axleInfo.leftWheel.steerAngle + wheelCorrection, Time.deltaTime * turnspeed);
                
                if (newWheelAngle > maxSteerAngle) newWheelAngle = maxSteerAngle;
                if (newWheelAngle < -maxSteerAngle) newWheelAngle = -maxSteerAngle;

                newWheelAngle = Mathf.Lerp(axleInfo.leftWheel.steerAngle, steering * maxSteerAngle, Time.deltaTime * turnspeed);

                axleInfo.leftWheel.steerAngle = newWheelAngle;
                axleInfo.rightWheel.steerAngle = newWheelAngle;
            }

            // set Deceleration force or braking 
            if (throttle == 0f && brake == 0f)
            {
                axleInfo.leftWheel.brakeTorque = decelerationForce;
                axleInfo.rightWheel.brakeTorque = decelerationForce;
            }
            else
            {
                axleInfo.leftWheel.brakeTorque = brake * breakForce;
                axleInfo.rightWheel.brakeTorque = brake * breakForce;
            }

            // update wheels
            UpdateSingleWheel(axleInfo.leftWheel, axleInfo.leftWheelTransform);
            UpdateSingleWheel(axleInfo.rightWheel, axleInfo.rightWheelTransform);
        }
    }

    private bool AWheelNotGrounded()
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (!axleInfo.leftWheel.isGrounded || !axleInfo.rightWheel.isGrounded)
            {
                return true;
            }
        }
        return false;
    }

    private bool AllWheelsNotGrounded()
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.leftWheel.isGrounded || axleInfo.rightWheel.isGrounded)
            {
                return false;
            }
        }
        return true;
    }

    private void ApplyBreaking(float brake)
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            axleInfo.leftWheel.brakeTorque = brake;
            axleInfo.rightWheel.brakeTorque = brake;
        }
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    public void ResetCar()
    {
        respawned = true;
        resetGrounded = true;
    }

    public (float,float,Vector3,Vector3) GetObservations()
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                return (car.velocity.magnitude * 3.6f / maxSpeed, axleInfo.leftWheel.steerAngle / maxSteerAngle, car.transform.position, car.transform.forward);
            }
        }
        return (0, 0, Vector3.zero, Vector3.zero);
    }
}

[System.Serializable]
public class AxleInfo
{
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public Transform leftWheelTransform;
    public Transform rightWheelTransform;
    public bool motor; // is this wheel attached to motor?
    public bool steering; // does this wheel apply steer angle?
}