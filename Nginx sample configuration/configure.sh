mkdir nginx
mkdir web-root
mkdir certbot-etc
mkdir certbot-var
mv ./nginx1.conf ./nginx/nginx.conf
docker compose up -d --force-recreate --no-deps nginx
docker compose run certbot certonly --webroot --webroot-path /var/www/html/ --email EMAIL --agree-tos --no-eff-email -d DOMAIN -d www.DOMAIN
docker compose stop nginx
rm ./nginx/nginx.conf -f
mv ./nginx2.conf ./nginx/nginx.conf
docker compose up -d --force-recreate --no-deps nginx
