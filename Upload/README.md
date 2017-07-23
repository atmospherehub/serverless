## Upload flow

This flows triggered by image that uploaded from atmosphere client device and takes care of the image by detecting faces, their moods and storing results in DB along with manipulating the image.

```
                                                       ++[queue] atmosphere+processed+images
                                                       |
                                                       |
                                                       |
+--------------------+        +--------------------+   +    +--------------------+
|    ImageUpload     +-------->    SendToEmotion   +--+?+--->    CleanupBlob     |
+--------------------+        +--------------------+        +---------+----------+
                                                                      +
                                                                      ?+--+[topic] atmosphere+images+with+faces
                                                                      +
                                                       +--------------+--------------+
                                                       |                             |
                                             +---------v----------+        +---------v----------+
                                             |     StoreTable     |        |     StoreSql       |
                                             +--------------------+        +---------+----------+
                                                                                     +
                                                                                     ?+--+[topic] atmosphere+images+in+db
                                                                                     +
                                                            +-------------------------------------------------+
                                                            |                        |                        |
                                                  +---------v----------+   +---------v----------+   +---------v----------+
                                                  |  CreateRectangles  |   | SendFaceForTagging |   |    CreateZoomIn    |
                                                  +--------------------+   +--------------------+   +--------------------+
```
