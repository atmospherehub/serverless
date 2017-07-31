## Reports flow

The flow is schedule based and generates reports.

```
+[timer] 
|
|     +--------------------+        +--------------------+
+?+--->   GenerateReport   +--+?+--->   SendEmailReport  |
      +--------------------+   +    +--------------------+
                               |
                               |
                               ++[topic] atmosphere+reports      
```
