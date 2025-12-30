using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


// 두개의 TextMeshProUGUI를 관리하는 클래스
public class TextMeshPro_OutlineObject : MonoBehaviour
{
    // 아웃라인이 되는 TextMeshProUGUI
    public TextMeshProUGUI outline;
    // 내용이 되는 TextMeshProUGUI
    public TextMeshProUGUI text;

    //     private void OnValidate()
    //     {
    // #if UNITY_EDITOR
    //         if (outline != null || this.text != null)
    //         {
    //             SetText(stringText);
    //         }
    // #endif
    //     }

    /// <summary>
    /// 텍스트를 설정하는 함수
    /// 인수로 받은 string문자를 각각의 TextMeshProUGUI로 설정한다
    /// </summary>
    /// <param name="text">설정할 텍스트</param>
    public void SetText(string text)
    {
        // 두개의 TextMeshProUGUI를 입력받은 string데이터로 설정한다
        this.text.text = text;
        outline.text = text;
    }

    /// <summary>
    /// 텍스트의 크기를 변경하는 함수
    /// 두 TextMeshProUGUI의 폰트 사이즈를 인수로 받은 사이즈로 변경한다
    /// </summary>
    /// <param name="size">변경하려는 폰트 사이즈</param>
    public void SetSize(float size)
    {
        // 두개의 TextMeshProUGUI의 폰트 사이즈를 입력받은 폰트 사이즈로 변경한다
        text.fontSize = size;
        outline.fontSize = size;
    }

    /// <summary>
    /// 텍스트의 색상을 변경하는 함수
    /// 두 TextMeshProUGUI의 폰트 색상을 인수로 받은 컬러값으로 변경한다
    /// </summary>
    /// <param name="color">변경하려는 폰트 컬러값</param>
    public void SetColor(Color color)
    {
        // 내용이 되는 텍스트는 입력받은 컬러값으로 변경한다
        text.color = color;
        // 아웃라인이 되는 텍스트의 경우 일반 색상은 기존 그대로, 아웃라인 색상만 입력받은 값으로 변경한다
        Color outColor = new Color(outline.color.r, outline.color.g, outline.color.b, color.a);
        outline.color = outColor;
    }

    /// <summary>
    /// 현재 일반 TextMeshProUGUI의 폰트 컬러값을 반환하는 함수
    /// </summary>
    /// <returns>일반 TextMeshProUGUI의 폰트 컬러값</returns>
    public Color GetColor()
    {
        // 일반 TextMeshProUGUI의 폰트 컬러값을 반환한다
        return text.color;
    }

    /// <summary>
    /// 텍스트의 정렬을 진행하는 함수
    /// </summary>
    /// <param name="align">정렬방식</param>
    public void SetAlign(TextAlignmentOptions align)
    {
        // 두개의 TextMeshProUGUI의 정렬 방식을 입력받은 값으로 설정한다
        text.alignment = align;
        outline.alignment = align;
    }
}