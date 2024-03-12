# NunitExtension

Problem: comparing two response:

Assert.That(actual, Is.EqualTo(expected));

Result:

Message: 
  Assert.That(actual, Is.EqualTo(expected))
  Expected: <NUnit.Extension.DeepCompare.Tests.RequestBodyTests+ResponseBody>
  But was:  <NUnit.Extension.DeepCompare.Tests.RequestBodyTests+ResponseBody>

Thanks.... I know what's wrong now...

But:
Assert.That(actual, Is.DeeplyEqualTo(expected));

"Property 'IsSuccess' mismatch: Expected 'False', but was 'True'."
