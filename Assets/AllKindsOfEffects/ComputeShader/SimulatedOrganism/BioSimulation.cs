using UnityEngine;

public class CreatureSimulation : MonoBehaviour
{
    public ComputeShader computeShader;
    public GameObject creaturePrefab;
    public int numCreatures = 1000;
    public float maxSpeed = 1f;
    public float canvasSize = 10f;

    private ComputeBuffer creatureBuffer;
    private ComputeBuffer newCreatureBuffer;

    public struct Creature
    {
        public Vector2 position; // 位置
        public Vector2 velocity; // 速度
    }
    private void Start()
    {
        InitializeBuffers();
        Simulate();
    }

    private void InitializeBuffers()
    {
        // 创建生物数组缓冲区
        creatureBuffer = new ComputeBuffer(numCreatures, sizeof(float) * 4); // 位置和速度共4个float
        newCreatureBuffer = new ComputeBuffer(numCreatures, sizeof(float) * 4); // 更新后的生物数据

        // 初始化生物数据
        Creature[] creatures = new Creature[numCreatures];
        for (int i = 0; i < numCreatures; i++)
        {
            creatures[i].position = new Vector2(Random.Range(-canvasSize, canvasSize), Random.Range(-canvasSize, canvasSize));
            creatures[i].velocity = Random.insideUnitCircle.normalized * maxSpeed;
        }

        // 将数据写入缓冲区
        creatureBuffer.SetData(creatures);
    }

    private void Simulate()
    {
        int kernelIndex = computeShader.FindKernel("CSMain");

        // 设置 Compute Shader 的参数
        computeShader.SetBuffer(kernelIndex, "creatures", creatureBuffer);
        computeShader.SetBuffer(kernelIndex, "newCreatures", newCreatureBuffer);
        computeShader.SetInt("canvasSize", Mathf.RoundToInt(canvasSize));

        // 执行 Compute Shader
        computeShader.Dispatch(kernelIndex, numCreatures / 64, 1, 1);

        // 获取更新后的生物数据
        Creature[] newCreatures = new Creature[numCreatures];
        newCreatureBuffer.GetData(newCreatures);

        // 实例化生物对象，可视化模拟结果
        for (int i = 0; i < numCreatures; i++)
        {
            GameObject creature = Instantiate(creaturePrefab, newCreatures[i].position, Quaternion.identity);
            // 这里你可以根据需要对生物对象进行进一步处理，比如设置速度方向、调整大小等
        }
    }

    private void OnDestroy()
    {
        // 释放 Compute Buffer 资源
        creatureBuffer.Release();
        newCreatureBuffer.Release();
    }
}
