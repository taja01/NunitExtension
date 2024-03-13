
# DeepCompare.NUnitExtension

This NUnit extension aids in the deep comparison of complex objects in unit tests.

When comparing complex object trees, NUnit's `Assert.AreEqual()` falls short in providing clear and useful assertion failure messages. This extension, through the `DeeplyWith` syntax, enhances how differences are reported for deep object graphs.

## Problem Statement

Consider two responses...

            var response1 = new ResponseBody
            {
                IsSuccess = true,
                Message = "Accepted",
                Method = Method.POST,
                StatusCode = 202,
                InnerMessage = new InnerMessage { Message = "Test" },
                Numbers = [1, 2, 3, 4, 5, 6]
            };
            var response2 = new ResponseBody
            {
                IsSuccess = true,
                Message = "Accepted",
                Method = Method.POST,
                StatusCode = 200,
                InnerMessage = new InnerMessage { Message = "Dev" },
                Numbers = [7, 6, 5, 4, 3, 2, 1]
            }; 
            
            Assert.That(response1, Is.EqualTo(response2));
            
            Message: 
            Assert.That(actual, Is.EqualTo(expected))
            Expected: <NUnit.Extension.DeepCompare.Tests.RequestBodyTests+ResponseBody>
            But was:  <NUnit.Extension.DeepCompare.Tests.RequestBodyTests+ResponseBody>          
            
## Solution

With the DeepCompare NUnit Extension, it is possible to compare these objects in a more intuitive way and get a more descriptive assert fail message...

            Assert.That(response1, Matches.DeeplyWith(response2));
            Message: 
            Differences found: 3. The details are as follows:
            Property 'StatusCode' mismatch: Expected '200', but was '202'.
            Property 'Numbers.Count' mismatch: Expected 'Count 7', but was 'Count 6'.
            Property 'InnerMessage.Message' mismatch: Expected 'Dev', but was 'Test'.

## Usage

Define two complex objects and use `Matches.DeeplyWith()` like shown in the example...

## Contribution

Feel free to create issue, or suggest fix.

## License

MIT License
