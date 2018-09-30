using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


/// <summary>
/// http://techblog.sega.jp/entry/2016/11/28/100000
/// </summary>
public class KeyReductionProcessor : AssetPostprocessor
{
    [MenuItem("Assets/Key Reduction")]
    static void KeyReduction()
    {
        Debug.Log("Key Reduction...");
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(AnimationClip), SelectionMode.Editable))
        {
            string path = AssetDatabase.GetAssetPath(obj);
            // AssetPathよりAnimationClipの読み込み
            AnimationClip anim_clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));

            foreach (var binding in AnimationUtility.GetCurveBindings(anim_clip).ToArray())
            {
                // AnimationClipよりAnimationCurveを取得
                AnimationCurve curve = AnimationUtility.GetEditorCurve(anim_clip, binding);
                // キーリダクションを行う
                AnimationCurveKeyReduction(curve);
                // AnimationClipにキーリダクションを行ったAnimationCurveを設定
                AnimationUtility.SetEditorCurve(anim_clip, binding, curve);
            }
            // AnimationClip名の作成
            string anim_clip_name = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path);
            // AnimationClipファイルの書き出し
            WriteAnimationCurve(anim_clip, anim_clip_name);
        }
    }

    // AnimationClipファイルの書き出し
    static private void WriteAnimationCurve(AnimationClip anim_clip, string anim_clip_name)
    {
        string tmp_name = anim_clip_name + "_tmp.anim"; // テンポラリファイル名
        // AnimationClipのコピーを作成
        var copyClip = Object.Instantiate(anim_clip);
        // テンポラリAnimationClipファイルの作成
        AssetDatabase.CreateAsset(copyClip, tmp_name);
        // テンポラリファイルから移し替え
        FileUtil.ReplaceFile(tmp_name, anim_clip_name + ".anim"); // コピー先ファイルがなければ自動で生成される。
        // テンポラリAnimationClipファイルの削除
        AssetDatabase.DeleteAsset(tmp_name);
        // データベースの更新
        AssetDatabase.Refresh();
    }

    // ２つのキーから、指定した時間の値を取得する
    static private float GetValueFromTime(Keyframe key1, Keyframe key2, float time)
    {
        float t;
        float a, b, c;
        float kd, vd;

        if (key1.outTangent == Mathf.Infinity) return key1.value; // コンスタント値

        kd = key2.time - key1.time;
        vd = key2.value - key1.value;
        t = (time - key1.time) / kd;

        a = -2 * vd + kd * (key1.outTangent + key2.inTangent);
        b = 3 * vd - kd * (2 * key1.outTangent + key2.inTangent);
        c = kd * key1.outTangent;

        return key1.value + t * (t * (a * t + b) + c);
    }

    // 指定したキーの値はKey1とKey2から得られた補間値と同じ値であるかを調べる
    static private bool IsInterpolationValue(Keyframe key1, Keyframe key2, Keyframe comp, float eps)
    {
        // 調査するキーのある位置
        var val1 = GetValueFromTime(key1, key2, comp.time);

        // 元の値からの差分の絶対値がしきい値以下であるか？
        if (eps < System.Math.Abs(comp.value - val1)) return false;

        // key1からcompの間
        var time = key1.time + (comp.time - key1.time) * 0.5f;
        val1 = GetValueFromTime(key1, comp, time);
        var val2 = GetValueFromTime(key1, key2, time);

        // 差分の絶対値がしきい値以下であるか？
        return (System.Math.Abs(val2 - val1) <= eps) ? true : false;
    }

    // 削除するインデックスリストの取得する。keysは３つ以上の配列
    static public IEnumerable<int> GetDeleteKeyIndex(Keyframe[] keys, float eps)
    {
        for (int s_idx = 0, i = 1; i < keys.Length - 1; i++)
        {
            // 前後のキーから補間した値と、カレントのキーの値を比較
            if (IsInterpolationValue(keys[s_idx], keys[i + 1], keys[i], eps))
            {
                yield return i; // 削除するインデックスを追加
            }
            else
            {
                s_idx = i; // 次の先頭インデックスに設定
            }
        }
    }

    // 入力されたAnimationCurveのキーリダクションを行う
    static public void AnimationCurveKeyReduction(AnimationCurve in_curve, float eps = 0.0001f)
    {
        if (in_curve.keys.Length <= 2) return; // Reductionの必要なし

        // 削除インデックスリストの取得
        var del_indexes = GetDeleteKeyIndex(in_curve.keys, eps).ToArray();

        // 不要なキーを削除する
        foreach (var del_idx in del_indexes.Reverse()) in_curve.RemoveKey(del_idx);
    }
}
