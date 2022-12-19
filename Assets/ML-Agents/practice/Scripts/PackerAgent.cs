using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

public class PackerAgent : Agent

{
    public GameObject ground;
    [HideInInspector]
    public Bounds areaBounds;



    //public List<GameObject> boxList = List(box1, box2);
    //the quantity of boxes should be known, not 
    [HideInInspector]
    public List<Transform> boxPool;

    // public GameObject block;
    // public GameObject block1;

    //cache each box on initialization
    // foreach (var box in BoxList) {
    //     RigidBody box;
    // }

    // Rigidbody m_BlockRb;  //cached on initialization
    // Rigidbody m_Block1Rb;  //cached on initialization

    [HideInInspector]
    public BinDetect binDetect;

    // [HideInInspector]
    // public GameObject carriedObject;

    [Header("Walk Speed")]
    [Range(0.1f, 10)]
    [SerializeField]
    //The walking speed to try and achieve
    private float m_TargetWalkingSpeed = 10;

    public float MTargetWalkingSpeed // property
    {
        get { return m_TargetWalkingSpeed; }
        set { m_TargetWalkingSpeed = Mathf.Clamp(value, .1f, m_maxWalkingSpeed); }
    }

    const float m_maxWalkingSpeed = 10; //The max walking speed

    //Should the agent sample a new goal velocity each episode?
    //If true, walkSpeed will be randomly set between zero and m_maxWalkingSpeed in OnEpisodeBegin()
    //If false, the goal velocity will be walkingSpeed
    public bool randomizeWalkSpeedEachEpisode;

    //The direction an agent will walk during training.
    private Vector3 m_WorldDirToWalk = Vector3.right;

    [HideInInspector]
    public Transform target; //Target the agent will walk towards during training.

    [Header("Body Parts")] public Transform hips;
    public Transform chest;
    public Transform spine;
    public Transform head;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;



    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    OrientationCubeController m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    JointDriveController m_JdController;
    EnvironmentParameters m_ResetParams;
    Box m_Box;

    public override void Initialize()
    {
        foreach (var box in boxPool) {
            m_Box.SetupBox(box);
            
        }
        // m_BlockRb = block.GetComponent<Rigidbody>();
        // m_Block1Rb = block.GetComponent<Rigidbody>();
         // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        binDetect = block1.GetComponent<BinDetect>();
        binDetect.agent = this;

        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
        m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();

        //Setup each body part
        m_JdController = GetComponent<JointDriveController>();
        m_JdController.SetupBodyPart(hips);
        m_JdController.SetupBodyPart(chest);
        m_JdController.SetupBodyPart(spine);
        m_JdController.SetupBodyPart(head);
        m_JdController.SetupBodyPart(thighL);
        m_JdController.SetupBodyPart(shinL);
        m_JdController.SetupBodyPart(footL);
        m_JdController.SetupBodyPart(thighR);
        m_JdController.SetupBodyPart(shinR);
        m_JdController.SetupBodyPart(footR);
        m_JdController.SetupBodyPart(armL);
        m_JdController.SetupBodyPart(forearmL);
        m_JdController.SetupBodyPart(handL);
        m_JdController.SetupBodyPart(armR);
        m_JdController.SetupBodyPart(forearmR);
        m_JdController.SetupBodyPart(handR);

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// Cannot overlap with the agent or overlap with the bin area
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);

            var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    void ResetBlock()
    {
        // Get a random position for the block.
        block.transform.position = GetRandomSpawnPos();
        block1.transform.position = GetRandomSpawnPos();

        // Reset box velocity and angularVelocity back to zero.
        foreach (var box in BoxList) {
            box.velocity = Vector3.zero;
            box.angularVelocity = Vector3.zero;
        }

        // Reset block angularVelocity back to zero.
        // m_BlockRb.angularVelocity = Vector3.zero;
        // m_Block1Rb.angularVelocity = Vector3.zero;
    }


    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        ResetBlock();
        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        hips.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            randomizeWalkSpeedEachEpisode ? Random.Range(0.1f, m_maxWalkingSpeed) : MTargetWalkingSpeed;

        SetResetParameters();
    }

    /// <summary>
    /// Add relevant information on each box to observation
    /// <summary>
    public void CollectObservationBox(box, VectorSensor sensor) {
        //box size, box location, box mass?, etc.

    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.touchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.position - hips.position));

        if (bp.rb.transform != hips && bp.rb.transform != handL && bp.rb.transform != handR)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }

    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = m_OrientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * MTargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(avgVel));
        //vel goal relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(velGoal));

        //rotation deltas
        sensor.AddObservation(Quaternion.FromToRotation(hips.forward, cubeForward));
        sensor.AddObservation(Quaternion.FromToRotation(head.forward, cubeForward));


        //position of target relative to target
        if (target!=null) {
            sensor.AddObservation(m_OrientationCube.transform.InverseTransformPoint(target.transform.position));
        }

        //observation of each body part
        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }

        //observation of boxes when agent does not have a box
        //if (!pickupScript.isHeld) {
            foreach (var box in BoxList) {
                CollectObservationBox(box, sensor);
            }
        //}
        //observation of boxes organized in bin when agent has a box
        // else {
        //     foreach (var box in BoxList) {
        //         PickupScript pickupScript = box.GetComponent<Collider>().gameObject.GetComponent<PickupScript>();
        //         if (pickupScript.isOrganized) {
        //             CollectObservationBox(box, sensor);
        //         }
        //     }
        // }
    }
    
    public void SelectTarget(int x) {
    	RaycastHit hits;
        if (target==null) {
            //TBD: add a condition if agent wants to pick up a box (based on leftover bin space for example)
            // do a ray search on all objects
            hits = Physics.RaycastAll(transform.position, transform.forward, Mathf.Infinity);
            //of all the available objects in the agent's field of vision, check for the ones marked for pick up (boxes)
            // will move this to state
            availableBoxes = new List<int>();
             for (int i = 0; i < hits.Length; i++) {
             	RaycastHit hit = hits[i];
            	PickupScript pickupScript = hit.collider.gameObject.GetComponent<PickupScript>();
            	//will change this
                if (pickupScript != null && !pickupScript.isHeld && !pickupScript.isOrganized) {
                    //TBD: label the "hit" as target and  have the agent walk towards the target
                    availableBoxes.Add(i);
              	}	
              }
              //mark the box as target
              target = hits[availableBoxes[x]].transform;
         }
   }

    public void PickUpTarget() {

        //if the agent touches target
        PickupScript pickupScript = target.GetComponent<Collider>().gameObject.GetComponent<PickupScript>();
        if (pickupScript!=null && !target.isOrganized) {
            //Pick up the target
            pickUpScript.isHeld = true;
            target.position = transform.position + ransform.forward * 0.5f;
        }
        
    }
        

      
        
        
    
    public void DropBox(int x) {
        if (target!=null) {
            //TBD:  if agent wants to drop the box

            //drop the box 
            PickupScript pickupScript = target.GetComponent<Collider>().gameObject.GetComponent<PickupScript>();
            pickupScript.isHeld = false;
            pickupScript.isOrganized = true;
            //pickupScript.gameObject.setActive(true);
            target.position = transform.position + transform.forward * 0.5f;
            target = null;

        }
        
    }
    
   


	////This is where the agent learns to move its joints and where it learns what is its next target to pick

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {

        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        var continuousActions = actionBuffers.ContinuousActions;
        
        SelectBox(continuousActions[++i]);        
        
        bpDict[chest].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[spine].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[thighL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[thighR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[shinL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[shinR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[footR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[footL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[armL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[armR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[forearmL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[forearmR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[head].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);

        //update joint strength settings
        bpDict[chest].SetJointStrength(continuousActions[++i]);
        bpDict[spine].SetJointStrength(continuousActions[++i]);
        bpDict[head].SetJointStrength(continuousActions[++i]);
        bpDict[thighL].SetJointStrength(continuousActions[++i]);
        bpDict[shinL].SetJointStrength(continuousActions[++i]);
        bpDict[footL].SetJointStrength(continuousActions[++i]);
        bpDict[thighR].SetJointStrength(continuousActions[++i]);
        bpDict[shinR].SetJointStrength(continuousActions[++i]);
        bpDict[footR].SetJointStrength(continuousActions[++i]);
        bpDict[armL].SetJointStrength(continuousActions[++i]);
        bpDict[forearmL].SetJointStrength(continuousActions[++i]);
        bpDict[armR].SetJointStrength(continuousActions[++i]);
        bpDict[forearmR].SetJointStrength(continuousActions[++i]);
    }

    //Update OrientationCube and DirectionIndicator
    void UpdateOrientationObjects()
    {
        m_WorldDirToWalk = target.position - hips.position;
        m_OrientationCube.UpdateOrientation(hips, target);
        if (m_DirectionIndicator)
        {
            m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
        }
    }

    void FixedUpdate()
    {
        UpdateOrientationObjects();

        var cubeForward = m_OrientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * MTargetWalkingSpeed, GetAvgVelocity());

        //Check for NaNs
        if (float.IsNaN(matchSpeedReward))
        {
            throw new ArgumentException(
                "NaN in moveTowardsTargetReward.\n" +
                $" cubeForward: {cubeForward}\n" +
                $" hips.velocity: {m_JdController.bodyPartsDict[hips].rb.velocity}\n" +
                $" maximumWalkingSpeed: {m_maxWalkingSpeed}"
            );
        }

        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = (Vector3.Dot(cubeForward, head.forward) + 1) * .5F;

        //Check for NaNs
        if (float.IsNaN(lookAtTargetReward))
        {
            throw new ArgumentException(
                "NaN in lookAtTargetReward.\n" +
                $" cubeForward: {cubeForward}\n" +
                $" head.forward: {head.forward}"
            );
        }

        AddReward(matchSpeedReward * lookAtTargetReward);
    }

    //Returns the average velocity of all of the body parts
    //Using the velocity of the hips only has shown to result in more erratic movement from the limbs, so...
    //...using the average helps prevent this erratic movement
    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in m_JdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    //normalized value of the difference in avg speed vs goal walking speed.
    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, MTargetWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);
    }

    /// <summary>
    ////Agent touched the target
    ///may need to change to when the distance is close enough so agent does not bump into it and fall down
    ///</summary>
     public void TouchedTarget()
     {
         AddReward(1f);
         print("Got to box!!!!!");
     }

    


    /// <summary>
    ////Agent got to the bin
    ///</summary>
    public void ScoredAGoal()
    { 
        // We use a reward of 5.
        AddReward(5f);
        print("Box in bin!!!");

        // By marking an agent as done AgentReset() will be called automatically.
        EndEpisode();
    }

    public void SetTorsoMass()
    {
        m_JdController.bodyPartsDict[chest].rb.mass = m_ResetParams.GetWithDefault("chest_mass", 8);
        m_JdController.bodyPartsDict[spine].rb.mass = m_ResetParams.GetWithDefault("spine_mass", 8);
        m_JdController.bodyPartsDict[hips].rb.mass = m_ResetParams.GetWithDefault("hip_mass", 8);
    }

    public void SetResetParameters()
    {
        SetTorsoMass();
    }
}




////1. if the walker falls down, episode ends, cannot have walker fall down when pumping into objects (he has to learn to avoid objects first
)
////2. 