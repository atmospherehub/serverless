## Tagging flow

The flow responseble for sending faces into slack channel to allow users to tag. Once the person is tagged the image then sent to cloud provider for model training.

```
 ?+--+[topic] atmosphere+images+in+db
 +
 |     +-----------------------+
 +?+---> RequestTaggingOnSlack |
       +-----------------------+
 
 
                                                        ++[topic] atmosphere-face-recognition
                                                        |
                                                        |
 +--------------------+        +--------------------+   +    +------------------------+
 |       FaceTag      +--+?+--->   StoreFaceTagSql  +--+?+--->  SendFaceForTraining   |
 +--------------------+   +    +--------------------+        +------------------------+
                          |
                          |
                         ++[topic] atmosphere+face+tagging

```
