using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Original_Animal
{
    public Vector2Int positionHead;

    public Vector2Int positionBody_A;
    public Vector2Int positionBody_B;
    // Start is called before the first frame update
    public Original_Animal(Vector2Int pos)
    {
        positionHead = pos;
        positionBody_A = new Vector2Int(pos.x+1, pos.y);
        positionBody_B = new Vector2Int(pos.x+2, pos.y);
    }
    
    public int GenerateRandomNumber(int min, int max)
    {
        int randomNumber = UnityEngine.Random.Range(min, max);
        return randomNumber;
    }
}
