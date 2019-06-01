const router = require('express').Router();
const static = require('../controllers/images.js');
const dateString = require('../components/dateString.js');

router.route('/').get(async (req, res) => {
  console.log(dateString(), '-', req.method, req.originalUrl, req.headers.host);
  let data = await static(req.originalUrl);
  res.status(data.statuscode);
  if (data.statuscode === 200) res.type(data.type);
  res.end();
});

module.exports = router;