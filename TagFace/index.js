module.exports = function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request');
    context.log(req.body);
    const tagAction = decodeRequest(req.body, "payload");
    const parsonId = tagAction.actions[0].selected_options[0].value;
    const imageId = tagAction.callback_id;

    context.bindings.outputSbMsg = { tagId: tagAction.action_ts, taggedPersonId: parsonId, imageId: imageId };
    context.log(context.bindings.outputSbMsg);    
    context.log(tagAction.original_message);
    tagAction.original_message.text = `bless you @${tagAction.user.name}, i welcome all others to help us get better even more`;
    tagAction.original_message.attachments[0].title = 'please identify the person in that picture';
    tagAction.original_message.attachments[1].actions[0].text = 'choose one..'
    
    context.res = { status: 200, body: tagAction.original_message };

    context.done();
};
var decodeRequest = function (body, paramName) {
    const encodedPayload = body.substring((paramName + "=").length);
    console.log(encodedPayload);
    var str = decodeURIComponent(encodedPayload);
    return JSON.parse(str);
}