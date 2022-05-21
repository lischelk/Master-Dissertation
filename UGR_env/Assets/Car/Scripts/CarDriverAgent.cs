using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.IO;
using System.Threading.Tasks;

public class CarDriverAgent : Agent
{
    [Header("Compare with/without future observations")]
    public bool testing = false;
    public string resultFile;

    [Header("Track Generator")]
    public TrackLayoutGenerator trackLayoutGenerator;
    public bool replay;

    [Header("Rewards")]
    public float CheckpointReward;
    public float lapReward;
    public float FinishReward;
    public float stepReward;
    public float ConeReward;
    public float notGroundedReward;
    public float allNotGroundedReward;

    [Header("Savepoint Settings")]
    public int amountOfSavepoints;
    public int metersBetweenSavepoints;
    public int metersSlideWindow;
    public bool smoothingByAverage;
    [Header("Savepoint Content")]
    public bool distanceNextSavepoint;
    public bool savepointPosition;
    public bool savepointRotation;
    public bool savepointRays;
    [Header("Savepoint Debug")]
    public bool debugSavepoints;
    public GameObject cube;
    public GameObject cube2;
    public List<float> rotationSavepoints;


    private CarController carController;
    private RayPerceptionSensor rayPerceptionSensor;
    private int detectableTags;
    private int amoutOfRays;

    private List<Vector3> trackRepresentationPosition = new List<Vector3>();
    private List<Vector3> trackRepresentationRotation = new List<Vector3>();
    private List<float[]> trackRepresentationSensorData = new List<float[]>();
    private List<GameObject> trackRepresentationCubes = new List<GameObject>();
    private List<GameObject> cubeAverages = new List<GameObject>();

    private bool firstLap;
    private bool finished;
    private Vector3 lastPosition;
    private int savepointIndex;
    private float[] nullFloatArray;
    private Vector3 offsetPosition;
    private float offsetRotation;

    private float lapTime;

    // Initialize when starting environment
    private void Start()
    {
        carController = GetComponent<CarController>();

        CheckpointSingle.OnCheckpointEnter += TrackCheckpoints_OnCarCorrectCheckpoint;
        CarController.NotGrounded += OnNotGrounded;
        CarController.AllNotGrounded += OnAllNotGrounded;
        TrackLayoutGenerator.Finished += Finished;

        detectableTags = FindObjectOfType<RayPerceptionSensorComponent3D>().DetectableTags.Count;
        rayPerceptionSensor = FindObjectOfType<RayPerceptionSensorComponent3D>().RaySensor;

        amoutOfRays = FindObjectOfType<RayPerceptionSensorComponent3D>().RaysPerDirection * 2 + 1;
        nullFloatArray = new float[(detectableTags + 2) * amoutOfRays];
        for (int i = 0; i < nullFloatArray.Length; i += (detectableTags + 2))
        {
            nullFloatArray[i + 2] = 1f;
            nullFloatArray[i + 3] = 1f;
        }

        savepointIndex = 0;
        if (debugSavepoints)
        {
            for (int i = 0; i < amountOfSavepoints; i++)
            {
                cubeAverages.Add(Instantiate(cube2, Vector3.zero, Quaternion.identity));
                cubeAverages[i].SetActive(false);
            }
        }
        finished = true;
    }

    // Update racetime
    private void Update()
    {
        lapTime += Time.deltaTime;
        AddReward(stepReward / MaxStep);
    }

    // When episode begins reset environment
    public override void OnEpisodeBegin()
    {
        MaxStep = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("max_steps", 3000);
        if (testing)
            _ = WriteFinishFile("----------------------------------------------");
        firstLap = true;
        ResetEnv(finished || !replay);
        //ResetEnv(finished);
        finished = false;
    }

    // Reset environment
    public void ResetEnv(bool reset = true)
    {
        (Vector3, Quaternion, Vector3, float) spawnPosition = trackLayoutGenerator.ResetTrack(reset, testing);
        carController.ResetCar();
        transform.SetPositionAndRotation(spawnPosition.Item1, spawnPosition.Item2);

        offsetPosition = spawnPosition.Item3;
        offsetRotation = spawnPosition.Item4;

        lapTime = 0f;
        lastPosition = new Vector3(0, 0, -100); //spawnPosition.Item1;
        savepointIndex = 0;

        trackRepresentationPosition.Clear();
        trackRepresentationRotation.Clear();
        trackRepresentationSensorData.Clear();

        if (debugSavepoints)
        {
            foreach (GameObject obj in trackRepresentationCubes)
            {
                Destroy(obj);
            }
            trackRepresentationCubes.Clear();
            foreach (GameObject obj in cubeAverages)
            {
                obj.SetActive(false);
            }
        }
    }

    // Driving over a finish line
    void Finished()
    {
        if (testing)
        {
            if (amountOfSavepoints > 0)
                _ = WriteFinishFile("- Finished lap " + (firstLap ? "1" : "2") + " after " + lapTime.ToString() + (!firstLap ? " using lap 1" : ""));
            else
                _ = WriteFinishFile("- Finished lap " + (firstLap ? "1" : "2") + " after " + lapTime.ToString());
        } else
        {
            if (amountOfSavepoints > 0)
                Debug.Log("Finished lap " + (firstLap ? "1" : "2") + " after " + lapTime.ToString() + (!firstLap ? " using lap 1" : ""));
            else
                Debug.Log("Finished lap " + (firstLap ? "1" : "2") + " after " + lapTime.ToString());
        }

        if (!firstLap)
        {
            AddReward(FinishReward);
            finished = true;
            EndEpisode();
        }
        else
        {
            AddReward(lapReward);
            firstLap = false;
            savepointIndex = 0;
            MoveSavepoints();
            if (debugSavepoints)
            {
                foreach (GameObject obj in cubeAverages)
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    // Move savepoints to second lap
    private void MoveSavepoints()
    {
        int savepoints = trackRepresentationPosition.Count;
        for (int i = 0; i < savepoints; i++)
        {
            // Lap 3
            trackRepresentationPosition.Add(Quaternion.Euler(0, offsetRotation * 2, 0) * trackRepresentationPosition[i] + Quaternion.Euler(0, offsetRotation, 0) * offsetPosition + offsetPosition);
            trackRepresentationRotation.Add(Quaternion.Euler(0, offsetRotation * 2, 0) * trackRepresentationRotation[i]);
            //_ = Instantiate(cube, trackRepresentationPosition[i + savepoints], Quaternion.identity);

            // Lap 2
            trackRepresentationPosition[i] = Quaternion.Euler(0, offsetRotation, 0) * trackRepresentationPosition[i] + offsetPosition;
            trackRepresentationRotation[i] = Quaternion.Euler(0, offsetRotation, 0) * trackRepresentationRotation[i];
            if (debugSavepoints)
            {
                GameObject CUBE = Instantiate(cube, trackRepresentationPosition[i], Quaternion.LookRotation(trackRepresentationRotation[i]));
                if (i > amountOfSavepoints - 1)
                    CUBE.SetActive(false);
                trackRepresentationCubes.Add(CUBE);
            }
        }
        if (debugSavepoints)
        {
            for (int i = savepoints; i < savepoints*2; i++)
            {
                GameObject CUBE = Instantiate(cube, trackRepresentationPosition[i], Quaternion.LookRotation(trackRepresentationRotation[i]));
                CUBE.SetActive(false);
                trackRepresentationCubes.Add(CUBE);
            }
        }
    }

    // Collect observations to give the model. (Exluding ray perception because these are added automaticly)
    public override void CollectObservations(VectorSensor sensor)
    {
        // Get necessary information of car (velocity, steering angle, global position, global direction)
        (float, float, Vector3, Vector3) carobs = carController.GetObservations();

        // Add 0 if first lap, else 1
        //sensor.AddObservation(firstLap ? 0 : 1);

        // Adding car velocity and steering angle to observations
        sensor.AddObservation(carobs.Item1);
        sensor.AddObservation(carobs.Item2);

        // Saving and using future observations
        if (amountOfSavepoints > 0)
        {
            // First time driving track --> collect observations
            if (firstLap && (lastPosition - carobs.Item3).magnitude > metersBetweenSavepoints)
                CreateSavepoint(carobs.Item3, carobs.Item4);

            // Add observations/Savepoints to sensor
            AddSavepoints(sensor, carobs.Item3, carobs.Item4, !firstLap);
        }
    }

    // Create savepoints in first lap
    public void CreateSavepoint(Vector3 globalPosition, Vector3 globalDirection)
    {
        lastPosition = globalPosition;
        trackRepresentationPosition.Add(lastPosition);
        if (savepointRotation)
            trackRepresentationRotation.Add(globalDirection);
        if (savepointRays)
            trackRepresentationSensorData.Add(GetRays());
    }

    // Add Savepoints information to sensor
    public void AddSavepoints(VectorSensor sensor, Vector3 globalPosition, Vector3 globalDirection, bool useFuture)
    {
        // Slide window if close to next Savepoint
        float fraction = 1f;
        if (useFuture)
        {
            float distance = (trackRepresentationPosition[savepointIndex] - globalPosition).magnitude;
            if (distance < metersSlideWindow)
            {
                savepointIndex++;
                distance = (trackRepresentationPosition[savepointIndex] - globalPosition).magnitude;
                if (debugSavepoints)
                {
                    trackRepresentationCubes[savepointIndex - 1].SetActive(false);
                    if (trackRepresentationCubes.Count > savepointIndex + amountOfSavepoints - 1)
                        trackRepresentationCubes[savepointIndex + amountOfSavepoints - 1].SetActive(true);
                }
            }
            if (smoothingByAverage)
                fraction = (distance - metersSlideWindow) / metersBetweenSavepoints;
        }

        // Add Savepoints
        for (int i = 0; i < amountOfSavepoints; i++)
        {
            // If next Savepoint exists
            if (useFuture && trackRepresentationPosition.Count > savepointIndex + i + 1)
            {
                if (i == 0 && distanceNextSavepoint) // Only of first Savepoint
                    sensor.AddObservation((trackRepresentationPosition[savepointIndex] - globalPosition).magnitude);
                if (savepointPosition)
                {
                    Vector3 positionAverage = fraction * trackRepresentationPosition[savepointIndex + i] + (1-fraction) * trackRepresentationPosition[savepointIndex + i + 1] - globalPosition;
                    sensor.AddObservation(positionAverage);
                }
                Quaternion rotationAverage = Quaternion.identity;
                if (savepointRotation)
                {
                    float obs = fraction * Vector3.SignedAngle(globalDirection, trackRepresentationRotation[savepointIndex + i], Vector3.up) + (1 - fraction) * Vector3.SignedAngle(globalDirection, trackRepresentationRotation[savepointIndex + i + 1], Vector3.up);
                    sensor.AddObservation(obs / 180);
                    if (debugSavepoints)
                    {
                        rotationSavepoints[i] = obs;
                        rotationAverage = Quaternion.LookRotation(fraction * trackRepresentationRotation[savepointIndex + i] + (1 - fraction) * trackRepresentationRotation[savepointIndex + i + 1]);
                    }
                }
                if (savepointRays)
                    sensor.AddObservation(trackRepresentationSensorData[savepointIndex + i]);

                if (debugSavepoints)
                {
                    Vector3 positionAverage = fraction * trackRepresentationPosition[savepointIndex + i] + (1 - fraction) * trackRepresentationPosition[savepointIndex + i + 1] - globalPosition;
                    cubeAverages[i].transform.SetPositionAndRotation(positionAverage + globalPosition, rotationAverage);
                }
            }

            // If next Savepoint doesn't exist add default values
            else
            {
                if (i == 0 && distanceNextSavepoint)
                    sensor.AddObservation(0f);
                if (savepointPosition)
                    sensor.AddObservation(Vector3.zero);
                if (savepointRotation)
                    sensor.AddObservation(2f);
                if (savepointRays)
                    sensor.AddObservation(nullFloatArray);

                if (debugSavepoints)
                    rotationSavepoints[i] = 2f;
            }
        }
    }

    // Get current ray observations
    public float[] GetRays()
    {
        RayPerceptionOutput.RayOutput[] rays = rayPerceptionSensor.RayPerceptionOutput.RayOutputs;
        float[] buffer = new float[(detectableTags + 2) * amoutOfRays];
        for (int i = 0; i < rays.Length; i++)
        {
            rays[i].ToFloatArray(detectableTags, i, buffer);
        }
        return buffer;
    }

    // Map output of model to actions
    public override void OnActionReceived(ActionBuffers actions)
    {
        float forwardAmount = 0f;
        float turnAmount = 0f;
        float brakeAmount = 0f;


        switch (actions.DiscreteActions[0])
        {
            case 0: forwardAmount = 0f; break;
            case 1: forwardAmount = +1f; break;
            case 2: brakeAmount = +1f; break;
        }

        switch (actions.DiscreteActions[1])
        {
            case 0: turnAmount = 0f; break;

            case 1: turnAmount = +0.25f; break;
            case 2: turnAmount = +0.5f; break;
            case 3: turnAmount = +0.75f; break;
            case 4: turnAmount = +1f; break;

            case 5: turnAmount = -0.25f; break;
            case 6: turnAmount = -0.5f; break;
            case 7: turnAmount = -0.75f; break;
            case 8: turnAmount = -1f; break;
        }

        carController.SetInputs(forwardAmount, turnAmount, brakeAmount);
    }

    // Take arrows keys as input in heuristic mode
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int forwardAction = 0;
        if (Input.GetKey(KeyCode.UpArrow)) forwardAction = 1; //Accelerate
        if (Input.GetKey(KeyCode.Space)) forwardAction = 2; //Brake

        int turnAction = 0;
        if (Input.GetKey(KeyCode.RightArrow)) turnAction = 4;
        if (Input.GetKey(KeyCode.LeftArrow)) turnAction = 8;

        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = forwardAction;
        discreteActions[1] = turnAction;
    }

    // Rewards
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<TrafficCone>(out _))
        {
            AddReward(ConeReward);
            if (testing)
            {
                _ = WriteFinishFile("- CONE");
            }
            EndEpisode();
        }
    }
    void TrackCheckpoints_OnCarCorrectCheckpoint()
    {
        AddReward(CheckpointReward);
    }
    void OnNotGrounded()
    {
        Debug.Log("Not Grounded");
        AddReward(notGroundedReward);
    }
    void OnAllNotGrounded()
    {
        if (testing)
        {
            _ = WriteFinishFile("- FLIP");
        }
        AddReward(allNotGroundedReward);
        EndEpisode();
    }

    // Write lap times to file
    public async Task WriteFinishFile(string line)
    {
        using StreamWriter file = new StreamWriter(resultFile, append: true);
        await file.WriteLineAsync(line);
    }
}
