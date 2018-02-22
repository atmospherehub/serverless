## Upload flow

This flows triggered by image that uploaded from atmosphere client device and takes care of the image by detecting faces, their moods and storing results in DB along with manipulating the image.

```
                                                            ++[queue] atmosphere+processed+images
+[HTTP]                                                     |
|                                                           |
|                                                           |
|    +--------------------+        +--------------------+   +    +--------------------+
+?+-->    ImageUpload     +-------->     DetectFaces    +--+?+--->    FacesSplitter   |
     +--------------------+        +--------------------+        +---------+----------+
                                                                           +
                                   atmosphere+images+with+faces [topic]+--+?
                                                                           +
                                                                           |
                                                                           |
                                                +-----------------------------------------------------+
                                                |                          |                          |
                                      +---------v----------+     +---------v----------+     +---------v----------+
                                      |      StoreSql      |     |  CreateRectangles  |     |    CreateZoomIn    |
                                      +---------+----------+     +--------------------+     +--------------------+
                                                +
                                                ?+--+[topic] atmosphere+images+in+db
                                                +


```
