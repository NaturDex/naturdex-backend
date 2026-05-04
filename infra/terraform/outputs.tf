output "app_server_public_ip" {
  value = aws_eip.app_static_ip.public_ip
  description = "The permanent public IP address of the server"
}