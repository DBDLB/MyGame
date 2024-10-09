using UnityEngine;

[ExecuteInEditMode]
public class LiquidScript : MonoBehaviour {

	public Material material;
	public Renderer meshRenderer;
	private bool skinnedTrans;
	private SkinnedMeshRenderer skinnedMeshRenderer;
	private Vector3 prevPos;
	private Quaternion prevRot;
	private Vector3 velocity;
	private Vector3 lerpedVelocity;
	private Vector3 prevVelocity;
	private Vector3 velocityVelocity; //lol, basically how much the velocity has changed at a given time
	private Vector3 angVelocity;
	private Vector3 lerpedAngVelocity;
	private Vector3 wobble;
	private Vector3 wobbleAmt;
	private Vector3 lerpedWobble;
	private float wobbleOffset;
	private float foam;
	private float waveIntensity = 0.85f;

    private float wavesMult = 1;
	private float volume = 0.5f;
	public Vector3 planePosition;
	public bool debugMode = false;
	public float foamSettleSpeed = 0.5f;
	public int Index =0;
	public int times = 2;

	private void Awake()
	{
		if (LiquidManagement.Instance)
		{
            //添加挂脚本的物体
			LiquidManagement.Instance.AddLiquid(gameObject);
			//LiquidManagement.Instance.LiquidObjectDestroyed += OnLiquidObjectDestroyed;
		}

	}

	// Use this for initialization
	void Start () {
		if (LiquidManagement.Instance)
		{
			LiquidManagement.Instance.SetUpdateTimeFrame(times);
		}

		// meshRenderer = GetComponent<Renderer>();
		// material = meshRenderer.material;
		wobbleAmt = Vector3.zero;
		wobbleOffset = Mathf.PI;
		prevPos = meshRenderer.bounds.center;
		prevRot = skinnedTrans ? transform.root.rotation : transform.rotation;
        wavesMult = 1;
    }

	// Update is called once per frame
	public void LiquidUpdate () {

		// This value prevents too uniform looking liquid surface movement
		wobbleOffset = Time.time * 0.5f;

		float objHeight = meshRenderer.bounds.max.y - meshRenderer.bounds.min.y;

		// Calculate positional velocity
		lerpedVelocity = Vector3.Lerp(lerpedVelocity, -velocityVelocity, Time.deltaTime * 20);
		Vector3 surfaceNormal = -Vector3.Lerp(lerpedVelocity, -Vector3.up, 0.5f);
		surfaceNormal = Quaternion.Euler(0, 180, 0) * surfaceNormal;


		// Calculate angular velocity
		Quaternion delta = (skinnedTrans ? transform.root.rotation : transform.rotation) * Quaternion.Inverse(prevRot);
		prevRot = skinnedTrans ? transform.root.rotation : transform.rotation;
		float angle;
		Vector3 axis;
		delta.ToAngleAxis(out angle, out axis);
		if (angle > 180f) angle -= 360f;
		angVelocity = (0.9f * Mathf.Deg2Rad * angle) * axis.normalized;
		lerpedAngVelocity = Vector3.Lerp(lerpedAngVelocity, angVelocity, Time.deltaTime * 5);
		lerpedAngVelocity = Vector3.Lerp(lerpedAngVelocity, -Vector3.up, 0.5f);
		lerpedAngVelocity = Quaternion.Euler(0, 180, 0) * lerpedAngVelocity;

		//Calculate surface wobble
		wobbleAmt = Vector3.Lerp(wobbleAmt, Vector3.zero, Time.deltaTime * 0.8f);
		float wobbleSpeed = Mathf.Pow(volume * 0.001f, 0.25f);
		//Debug.Log("对象的volume: " + volume);
		wobble = new Vector3(wobbleAmt.x, 1, wobbleAmt.z);
		lerpedWobble = Vector3.Lerp(lerpedWobble, wobble, Time.deltaTime * 5);
		float normAng = Vector3.Angle(Vector3.up, surfaceNormal);
		if (debugMode) {
			Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.up, Color.green);
			Debug.DrawRay(transform.position + Vector3.up * 2, surfaceNormal, Color.blue);
		}
		normAng = Mathf.Clamp(normAng, 0, 90f) * (90/100);
		Vector2 wobbleSins = (1 - normAng) * new Vector2(Mathf.Sin((Time.time + wobbleOffset) / wobbleSpeed), Mathf.Sin(Time.time / wobbleSpeed));
		wobble.x *= wobbleSins.x;
		wobble.z *= wobbleSins.y;
		wobble.x += surfaceNormal.x * 0.25f;
		wobble.z += surfaceNormal.z * 0.25f;

		// Create cutoff plane relative to the mesh's bound's center
		Vector3 center = meshRenderer.bounds.center;
		Plane plane = new Plane(wobble.normalized, center + new Vector3(planePosition.x, objHeight * planePosition.y, planePosition.z));
		Vector4 planeVec4 = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);

		// Calculate foam level
		foam = Mathf.Lerp(foam, velocityVelocity.sqrMagnitude, Time.deltaTime * foamSettleSpeed);
		foam = Mathf.Clamp(foam, 0, 1);
		// Set shader variables
		meshRenderer.sharedMaterial.SetVector("_Plane", planeVec4);
		wavesMult = Mathf.Lerp(wavesMult, Mathf.Pow(Mathf.Sqrt(wobbleAmt.magnitude * 1.25f) * 0.5f + 1, 3.5f), Time.deltaTime * 2);
		meshRenderer.sharedMaterial.SetFloat("_WavesMult", wavesMult);
		meshRenderer.sharedMaterial.SetFloat("_MeshScale", wobbleSpeed * 3);
		// material.SetFloat("_Foam", foam);
		wobbleAmt.x += (surfaceNormal.x * 0.5f);
		wobbleAmt.y += velocity.y * 0.02f;
		wobbleAmt.z += (surfaceNormal.z * 0.5f);
		wobbleAmt.x += lerpedAngVelocity.z * 10f;
		wobbleAmt.z += lerpedAngVelocity.x * 10f;
		wobbleAmt.x = Mathf.Clamp(wobbleAmt.x, -waveIntensity, waveIntensity);
		wobbleAmt.y = Mathf.Clamp(wobbleAmt.y, -waveIntensity, waveIntensity);
		wobbleAmt.z = Mathf.Clamp(wobbleAmt.z, -waveIntensity, waveIntensity);
	}

	public void FixedUpdate() {
		velocity = (transform.position - prevPos) * 50f;
		velocityVelocity = Vector3.Lerp(velocityVelocity, (prevVelocity - velocity), Time.fixedDeltaTime);
		prevPos = transform.position;
		prevVelocity = velocity;
	}

	Matrix4x4 GetRootTransformationMatrix(Transform trans) {
		Matrix4x4 m = Matrix4x4.TRS(meshRenderer.bounds.center, trans.rotation, trans.localScale);
		while (trans.parent != null) {
			trans = trans.parent;
			m.SetColumn(0, m.GetColumn(0) * trans.localScale.x);
			m.SetColumn(1, m.GetColumn(1) * trans.localScale.y);
			m.SetColumn(2, m.GetColumn(2) * trans.localScale.z);
		}
		return m;
	}

	float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3) {
		float v321 = p3.x * p2.y * p1.z;
		float v231 = p2.x * p3.y * p1.z;
		float v312 = p3.x * p1.y * p2.z;
		float v132 = p1.x * p3.y * p2.z;
		float v213 = p2.x * p1.y * p3.z;
		float v123 = p1.x * p2.y * p3.z;
		return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
	}

	float VolumeOfMesh(ref Mesh mesh) {
		float volume = 0;
		Vector3[] vertices = mesh.vertices;
		Matrix4x4 m = GetRootTransformationMatrix(transform);
		for (int i = 0; i < vertices.Length; i++)
			vertices[i] = m.MultiplyPoint3x4(vertices[i]);
		int[] triangles = mesh.triangles;
		for (int i = 0; i < mesh.triangles.Length; i += 3) {
			Vector3 p1 = vertices[triangles[i]];
			Vector3 p2 = vertices[triangles[i + 1]];
			Vector3 p3 = vertices[triangles[i + 2]];
			if (debugMode) {
				Debug.DrawLine(p1, p2);
				Debug.DrawLine(p2, p3);
				Debug.DrawLine(p3, p1);
			}
			volume += SignedVolumeOfTriangle(p1, p2, p3);
		}
		return Mathf.Abs(volume);
	}

	public void RecalculateVolume() {
		Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
		volume = VolumeOfMesh(ref mesh);
	}

	/*void OnLiquidObjectDestroyed(GameObject obj)
	{
		if (LiquidManagement.Instance)
		{
			LiquidManagement.Instance.DestoryLiquid(gameObject);

			// 取消订阅事件，以防重复执行
			LiquidManagement.Instance.LiquidObjectDestroyed -= OnLiquidObjectDestroyed;
		}


	}*/
}
