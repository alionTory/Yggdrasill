using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using TMPro;

public class HelloWorldTest
{
    [UnityTest]
    public IEnumerator HelloWorldTextExists()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;  // 씬 로드 대기
        
        var helloWorldText = GameObject.Find("Hello World Text");
        Assert.NotNull(helloWorldText, "Hello World Text game object must exist.");
        
        var tmp = helloWorldText.GetComponent<TextMeshProUGUI>();
        Assert.NotNull(tmp, "TextMeshProUGUI component must exist.");
        Assert.AreEqual("Hello World", tmp.text, "String must equal 'Hello World'.");
    }
    
    
}
