using UnityEngine;
using System.Collections;

public class Car : MonoBehaviour, ICar {
	public WheelCollider[] wheelColliders;
	public Transform[] wheelMeshes;

	public float maxTorque = 50f;
	public float maxSpeed = 10f;

	public Transform centrOfMass;

	public float requestTorque = 0f;
	public float requestBrake = 0f;
	public float requestSteering = 0f;

	public Vector3 acceleration = Vector3.zero;
	public Vector3 velocity = Vector3.zero;

	public Vector3 startPos;
	public Quaternion startRot;
	private Quaternion gyro = Quaternion.identity;
	public float length = 1.7f;

	public Rigidbody rb;

	//for logging
	public float lastSteer = 0.0f;
	public float lastAccel = 0.0f;

	//when the car is doing multiple things, we sometimes want to sort out parts of the training
	//use this label to pull partial training samples from a run 
	public string activity = "keep_lane";

	public float maxSteer = 16.0f;
	public float SteerSpeed = 10000000.0f;

	//name of the last object we hit.
	public string last_collision = "none";

	private bool after_reset = false;

	void Awake() {
		rb = GetComponent<Rigidbody>();

		if (rb && centrOfMass) {
			rb.centerOfMass = centrOfMass.localPosition;
		}

		requestTorque = 0f;
		requestSteering = 0f;

		SavePosRot();
	}

	public void SavePosRot() {
		startPos = transform.position;
		startRot = transform.rotation;
	}

	public void RestorePosRot() {
		after_reset = true;
		Set(startPos, startRot);
	}

	public void RequestThrottle(float val) {
		requestTorque = val;
		requestBrake = 0f;
	}

	public void SetMaxSteering(float val) {
		maxSteer = val;
	}

	public float GetMaxSteering() {
		return maxSteer;
	}

	public void RequestSteering(float val) {
		requestSteering = Mathf.Clamp(val, -maxSteer, maxSteer);
	}

	public void Set(Vector3 pos, Quaternion rot) {
		rb.position = pos;
		rb.rotation = rot;

		transform.position = pos;
		transform.rotation = rot;
	}

	public float GetSteering() {
		return requestSteering;
	}

	public float GetThrottle() {
		return requestTorque;
	}

	public float GetFootBrake() {
		return requestBrake;
	}

	public float GetHandBrake() {
		return 0.0f;
	}

	public Vector3 GetVelocity() {
		return velocity;
	}

	public Vector3 GetAccel() {
		return acceleration;
	}
	public Quaternion GetGyro() {
		return gyro;
  	}
	public float GetOrient () {
		Vector3 dir = this.transform.forward;
		return Mathf.Atan2(dir.z, dir.x);
	}

	public Transform GetTransform() {
		return transform;
	}

	public bool IsStill() {
		return rb.IsSleeping();
	}

	public void RequestFootBrake(float val) {
		requestBrake = val;
	}

	public void RequestHandBrake(float val) {
		//todo
	}
	
	// Update is called once per game frame, this frequency can vary
	void Update () {
		UpdateWheelPositions();
	}

	public string GetActivity() {
		return activity;
	}

	public void SetActivity(string act) {
		activity = act;
	}

	// FixedUpdate is called 50 times per second and this can not be changed
	void FixedUpdate() {
		if (after_reset) {
			Set(startPos, startRot);
			rb.velocity = Vector3.zero;
			velocity = Vector3.zero;
			gyro = Quaternion.identity;
			requestTorque = 0f;
			requestBrake = 0f;
			requestSteering = 0f;

			for (int i = 0; i < wheelColliders.Length; i++) {
				WheelCollider wc = wheelColliders[i];
				wc.motorTorque = 0f;
				wc.brakeTorque = 0f;
			}

			after_reset = false;
		}

		lastSteer = requestSteering;
		lastAccel = requestTorque;

		float throttle = requestTorque * maxTorque;
		float brake = requestBrake;

		//front two tires.
		wheelColliders[2].steerAngle = requestSteering;
		wheelColliders[3].steerAngle = requestSteering;

		Vector3 prevVel = velocity;
		velocity = transform.InverseTransformDirection(rb.velocity);
		acceleration = (velocity - prevVel)/Time.deltaTime;
		gyro = rb.rotation * Quaternion.Inverse(rb.rotation);

		//four wheel drive at the moment
		for (int i = 0; i < wheelColliders.Length; i++) {
			WheelCollider wc = wheelColliders[i];

			if (rb.velocity.magnitude < maxSpeed) {
				wc.motorTorque = throttle;
			} else {
				wc.motorTorque = 0.0f;
			}

			wc.brakeTorque = 400f * brake;
		}


	}

	void FlipUpright() {
		Quaternion rot = Quaternion.Euler(180f, 0f, 0f);
		transform.rotation *= rot;
		transform.position += Vector3.up * 2;
	}

	void UpdateWheelPositions() {
		Quaternion rot;
		Vector3 pos;

		for(int i = 0; i < wheelColliders.Length; i++) {
			WheelCollider wc = wheelColliders[i];
			Transform tm = wheelMeshes[i];

			wc.GetWorldPose(out pos, out rot);

			tm.position = pos;
			tm.rotation = rot;
		}
	}

	//get the name of the last object we collided with
	public string GetLastCollision() {
		return last_collision;
	}

	public void ClearLastCollision() {
		last_collision = "none";
	}

	void OnCollisionEnter(Collision col) {
		last_collision = col.gameObject.name;
	}
}
