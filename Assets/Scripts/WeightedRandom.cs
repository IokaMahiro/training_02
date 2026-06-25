using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 重み付きランダム選択ユーティリティ
/// </summary>
public static class WeightedRandom
{
    // ── float配列版 ──────────────────────────────────────────

    /// <summary>
    /// 重み配列からインデックスを1つ選ぶ
    /// </summary>
    public static int Pick(float[] weights)
    {
        if (weights == null || weights.Length == 0)
            throw new ArgumentException("weights is empty.");

        float total = 0f;
        foreach (var w in weights)
        {
            if (w < 0f) throw new ArgumentException("Weight must be >= 0.");
            total += w;
        }
        if (total <= 0f) throw new ArgumentException("Total weight must be > 0.");

        float roll = UnityEngine.Random.Range(0f, total);

        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return i;
        }

        return weights.Length - 1; // 浮動小数点誤差の保険
    }

    // ── List版 ───────────────────────────────────────────────

    /// <summary>
    /// 重みリストからインデックスを1つ選ぶ
    /// </summary>
    public static int Pick(List<float> weights)
        => Pick(weights.ToArray());

    // ── ジェネリック版（要素そのものを返す）──────────────────

    /// <summary>
    /// 要素リストと重みリストから要素を1つ選ぶ
    /// </summary>
    public static T Pick<T>(IList<T> items, IList<float> weights)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("items is empty.");
        if (items.Count != weights.Count)
            throw new ArgumentException("items and weights must have the same length.");

        float[] w = new float[weights.Count];
        for (int i = 0; i < weights.Count; i++) w[i] = weights[i];

        return items[Pick(w)];
    }

    // ── 複数抽選（重複あり）──────────────────────────────────

    /// <summary>
    /// 重み付きで n 回抽選し、インデックス配列を返す（重複あり）
    /// </summary>
    public static int[] PickMultiple(float[] weights, int count)
    {
        var result = new int[count];
        for (int i = 0; i < count; i++)
            result[i] = Pick(weights);
        return result;
    }
}