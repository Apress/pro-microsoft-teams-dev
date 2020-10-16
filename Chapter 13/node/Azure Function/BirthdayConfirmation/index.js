module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');
    var responseMessage = {"text": "Sorry you can not make it" }
    const name = (req.query.name || (req.body && req.body.name));
    if(req.query.answer != "no"){
        responseMessage = {"text": "Thanks for registering and giving the following comment: " + req.body }
    }
        //Here you should save this data to a repository. This can be a simple SharePoint list or a CosmosDB or SQL instance.
    context.res = {
        // status: 200, /* Defaults to 200 */
        headers: {
            "CARD-UPDATE-IN-BODY":"true"
        },
        body: responseMessage
    };
}