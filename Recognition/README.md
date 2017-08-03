## Tagging flow

The flow responseble for sending faces into slack channel to allow users to tag. Once the person is tagged the image then sent to cloud provider for model training.

```
                                 +-+[topic] atmosphere-images-in-db
                                 |
                 +---------------+----------------+
                 |                                |
      +----------v------------+       +-----------v-----------+
      | RequestTaggingOnSlack |       |     IdentifyFace      |
      +-----------------------+       +-----------------------+



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