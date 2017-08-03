## Tagging flow

The flow responseble for sending faces into slack channel to allow users to tag. Once the person is tagged the image then sent to cloud provider for model training.

```
           atmosphere-face-not-identified [queue]+-+
                                                   |      +-----------v-----------+
+[topic] atmosphere-images-in-db                +--?------> RequestTaggingOnSlack |
|                                               |         +-----------------------+
|   +-----------------------+                   |
+--->      IdentifyFace     +-------------------+
    +-----------------------+                   |
                                                |         +-----------------------+
                                                +---?----->   FinalizeIdentified  |
                                                    |     +-----------------------+
                atmosphere-face-identified [queue]+-+


                                                                   ++[queue] atmosphere+face+tagged
+[http]                                                            |
|                                                                  |
|     +-----------------------+        +-----------------------+   +    +-----------------------+
+?+--->         FaceTag       +--+?+--->   StoreFaceTagSql     +--+?+---> SendFaceForTraining   |
      +-----------------------+   +    +-----------------------+        +---------+--+--+-------+
                                  |                                               |  |  |
                                  ++[queue] atmosphere+face+tagging               |  |  |
                                                                                  |  |  |
                   +--------------------------------------------------------------+  |  |
                   |                                                                 +  |
                   |                          atmosphere+face+training+sent [queue]++?  |
                   +                                                                 +  |
                   ?++[queue] atmosphere+face+enrich+user                            |  |
                   +                                                                 |  |
                   |                              +----------------------------------+  |
                   |                              |                                     +
                   |                              +    atmosphere+face+cleanup [queue]++?
                   |                              ?                                     +
                   |                              +                                 +---+
                   |                              |                                 |
       +-----------v-----------+       +----------v------------+        +-----------v-----------+
       |      EnrichUser       |       |       FinalizeTag     |        |       CleanupTag      |
       +-----------------------+       +-----------------------+        +-----------------------+

+[timer]
|
|     +-----------------------+
+?+--->     StartTraining     |
      +-----------------------+


```
