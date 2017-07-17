# Gateway for Atmosphere stations and API


## Notes

- `funproj` files are excluded for now, currently the deployment script cannot handle them 

## Pipeline
```
                                                       ++[queue] atmosphere+processed+images
                                                       |
                                                       |
                                                       |
+--------------------+        +--------------------+   +    +--------------------+
|    FacesUpload     +-------->    ProcessImage    +--+?+--->  PostProcessImage  |
+--------------------+        +--------------------+        +---------+----------+
                                                                      +
                                                                      ?+--+[topic] atmosphere+images+with+faces
                                                                      +
                         +--------------+--------------+--------------+--------------+
                         |              |              |                             |
                         |              |    +---------v----------+        +---------v----------+
                         |              |    |     StoreTable     |        |     StoreSql       |
                         |              |    +--------------------+        +---------+----------+
                         |              |                                            +
                         |    +---------v----------+                                 ?+--+[topic] atmosphere+images+in+db
                         |    |    NotifySlack     |                                 +
                         |    +--------------------+        +-------------------------------------------------+
                         |                                  |                        |                        |
                         |                        +---------v----------+   +---------v----------+   +---------v----------+
                         |                        |   StoreRectangles  |   | SendFaceForTagging |   |     StoreZoomIn    |
               +---------v----------+             +--------------------+   +--------------------+   +--------------------+
               |     NotifyMQTT     |
               +--------------------+



+--------------------+        +--------------------+
|   GenerateReport   +--+?+--->   SendEmailReport  |
+--------------------+   +    +--------------------+
                         |
                         |
                         ++[topic] atmosphere+reports



+--------------------+        +-------------------+
|       FaceTag      +--+?+--->  StoreFaceTagSql  |
+--------------------+   +    +-------------------+
                         |
                         |
                         ++[topic] atmosphere+face+tagging


```
