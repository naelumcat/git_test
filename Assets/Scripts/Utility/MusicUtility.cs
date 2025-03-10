using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract class MusicUtility
{
    public static float TimeSamplesToTime(float timeSamples, float frequency)
    {
        // Time = TimeSamples / Frequency
        return timeSamples / frequency;
    }

    public static int TimeToTimeSamples(float time, float frequency)
    {
        // TimeSamples = Time * Frequency
        return (int)(time * frequency);
    }

    /// <summary>
    /// 현재 음악이 재생중인 시간과 다른 시간이 주어졌을때
    /// 현재 음악이 재생중인 시간을 기준으로 다른 시간을 판정선과의 거리 비율로 변환합니다.
    /// </summary>
    /// <param name="time">다른 시간</param>
    /// <param name="fieldLength">필드의 길이</param>
    /// <param name="playingTime">현재 재생중인 시간</param>
    /// <returns></returns>
    public static float TimeToRatio(float time, float fieldLength, float playingTime)
    {
        // Ratio = (Time - PlayingTime) * FieldLength
        float deltaTime = time - playingTime;
        float ratio = deltaTime * (float)fieldLength;
        return (float)ratio;
    }

    public static float RatioToTime(float ratio, float fieldLength, float playingTime)
    {
        // Ratio = (Time - PlayingTime) * FieldLength
        // Ratio = Time * FieldLength - PlayingTime * FieldLength
        // Time = (Ratio + PlayingTime * FieldLength) / FieldLength
        return (ratio + playingTime * fieldLength) / fieldLength;
    }

    public static void ExtractAudioVolumes(AudioClip clip, out float[] left, out float[] right)
    {
        float[] data = new float[clip.channels * clip.samples];

        if (clip.channels == 1)
        {
            left = new float[clip.samples];
            right = new float[0];
            clip.GetData(left, 0);
        }
        else
        {
            left = new float[clip.samples];
            right = new float[clip.samples];
            clip.GetData(data, 0);

            int idx = 0;
            for (int i = 0; i < data.Length - 1; i += 2)
            {
                left[idx] = data[i];
                right[idx] = data[i + 1];
                ++idx;
            }
        }
    }

    public static void TimeToWatchTime(float time, out int minute, out int second, out int milliSecond)
    {
        minute = (int)(time / 60);
        second = (int)(time - minute * 60.0f);
        milliSecond = (int)((time - minute * 60.0f - second) * 1000.0f);
    }

    public static float WatchTimeToTime(int minute, int second, int milliSecond)
    {
        return minute * 60.0f + second + (float)(milliSecond * 0.001f);
    }
}
