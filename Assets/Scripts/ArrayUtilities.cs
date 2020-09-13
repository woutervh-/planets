using UnityEngine;

public class ArrayUtilities
{
    public static void Shuffle<T>(T[] array, int seed)
    {
        // Fisher–Yates
        Random.InitState(seed);
        for (int i = array.Length - 1; i >= 1; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}
