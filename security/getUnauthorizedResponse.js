function getUnauthorizedResponse(req) {
  return req.auth ? '<h1>401 Unauthorized</h1><p>Credentials rejected</p>' : '<h1>401 Unauthorized</h1><p>No credentials provided</p>'
}

module.exports = getUnauthorizedResponse;